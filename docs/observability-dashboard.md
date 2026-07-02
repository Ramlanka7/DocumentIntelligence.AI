# Observability — Dashboard Queries and Key Metrics

This document describes the key metrics, log queries, and dashboard panels used to monitor the
AI Document Intelligence Platform in production.

---

## 1. Stack

| Signal    | Source                                  | Export target(s)                                   |
|-----------|-----------------------------------------|----------------------------------------------------|
| Logs      | Serilog (structured JSON)               | Console, Azure Monitor (App Insights) via OTel     |
| Traces    | OpenTelemetry (ASP.NET Core + HTTP)     | OTLP collector, Azure Monitor                      |
| Metrics   | OpenTelemetry + custom `Meter`          | OTLP collector, Azure Monitor                      |
| Health    | ASP.NET Core HealthChecks               | `/health`, `/health/live`, `/health/ready`          |

### Configuration flags

| Setting                             | Effect                                            |
|-------------------------------------|---------------------------------------------------|
| `ApplicationInsights:ConnectionString` | Enables Azure Monitor (App Insights) export    |
| `OTEL_EXPORTER_OTLP_ENDPOINT`       | Enables OTLP export (e.g. to Grafana/Jaeger)       |

If neither is set, traces and metrics are still collected locally but not exported.
Logs always go to the console.

---

## 2. Custom Metrics

All custom metrics are emitted from the `AI.DocumentIntelligence.Infrastructure` meter.

| Metric name                    | Type      | Unit      | Description                                      |
|-------------------------------|-----------|-----------|--------------------------------------------------|
| `ai.completion.requests`      | Counter   | requests  | Total AI completion calls dispatched             |
| `ai.completion.duration_ms`   | Histogram | ms        | End-to-end latency of AI completions             |
| `ai.tokens.consumed`          | Counter   | tokens    | Total LLM tokens consumed (prompt + completion)  |
| `ai.completion.cost_usd`      | Histogram | USD       | Estimated USD cost per completion call           |
| `search.requests`             | Counter   | requests  | Total Azure AI Search calls dispatched           |
| `search.duration_ms`          | Histogram | ms        | End-to-end latency of Azure AI Search calls      |

ASP.NET Core built-in metrics (via `AddAspNetCoreInstrumentation`):

| Metric name                       | Description                          |
|-----------------------------------|--------------------------------------|
| `http.server.request.duration`    | HTTP request duration (histogram)    |
| `http.server.active_requests`     | Active in-flight requests (gauge)    |
| `http.server.request.body.size`   | HTTP request body size               |

---

## 3. Health Check Endpoints

| Endpoint        | Checks                             | Use-case                           |
|-----------------|------------------------------------|------------------------------------|
| `/health/live`  | None (process ping only)           | Kubernetes `livenessProbe`         |
| `/health/ready` | database, azure-search, ai-provider| Kubernetes `readinessProbe`        |
| `/health`       | All registered checks              | Full operator overview             |

Response shape (JSON):
```json
{
  "status": "Healthy | Degraded | Unhealthy",
  "duration": 42.3,
  "checks": [
    {
      "name": "database",
      "status": "Degraded",
      "description": "Database connection string is not configured (pending T02).",
      "duration_ms": 0.1
    }
  ]
}
```

**Status interpretation:**
- `Healthy` — component is reachable and responding normally.
- `Degraded` — component is not configured or is responding slowly; the application can still serve traffic.
- `Unhealthy` — component is configured but unreachable; the application cannot serve affected features.

---

## 4. Key Kusto (Application Insights) Queries

### 4.1 HTTP Request Latency — p50 / p95 / p99

```kusto
requests
| where timestamp > ago(1h)
| where name !startswith "GET /health"
| summarize
    p50 = percentile(duration, 50),
    p95 = percentile(duration, 95),
    p99 = percentile(duration, 99),
    count = count()
  by bin(timestamp, 5m), name
| order by timestamp desc
```

### 4.2 Error Rate by Endpoint

```kusto
requests
| where timestamp > ago(1h)
| where resultCode !startswith "2"
| summarize
    error_count = count(),
    total = countif(true)
  by bin(timestamp, 5m), name, resultCode
| extend error_rate = todouble(error_count) / todouble(total) * 100
| order by error_rate desc
```

### 4.3 AI Completion Token Usage (Custom Metrics)

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "ai.tokens.consumed"
| summarize
    total_tokens = sum(valueSum),
    avg_per_call = avg(valueSum)
  by bin(timestamp, 1h)
| order by timestamp desc
```

### 4.4 AI Completion Latency Trend

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "ai.completion.duration_ms"
| summarize
    p50_ms = percentile(value, 50),
    p95_ms = percentile(value, 95),
    call_count = count()
  by bin(timestamp, 15m)
| order by timestamp desc
```

### 4.5 Estimated AI Cost per Hour (USD)

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "ai.completion.cost_usd"
| summarize
    total_cost_usd = sum(valueSum),
    call_count = count()
  by bin(timestamp, 1h)
| order by timestamp desc
```

### 4.6 Azure AI Search Request Volume

```kusto
customMetrics
| where timestamp > ago(24h)
| where name == "search.requests"
| summarize total_searches = sum(valueCount) by bin(timestamp, 1h)
| order by timestamp desc
```

### 4.7 Correlated Trace + Log Lookup

```kusto
// Find all log events for a specific correlation ID
traces
| where timestamp > ago(1h)
| where customDimensions.CorrelationId == "<paste-id-here>"
| project timestamp, message, severityLevel, customDimensions
| order by timestamp asc
```

### 4.8 Failed MediatR Commands

```kusto
traces
| where timestamp > ago(1h)
| where message has "failed with"
| parse message with RequestName " failed with " ErrorCode ": " ErrorDescription
| summarize failures = count() by RequestName, ErrorCode, bin(timestamp, 15m)
| order by failures desc
```

---

## 5. Grafana / OTLP Dashboard Panels (when using OTLP export)

The following PromQL queries apply when metrics are scraped via Prometheus-compatible OTLP collector.

### HTTP Request Rate
```promql
sum(rate(http_server_request_duration_ms_count[5m])) by (http_route)
```

### HTTP P95 Latency
```promql
histogram_quantile(0.95, sum(rate(http_server_request_duration_ms_bucket[5m])) by (le, http_route))
```

### AI Token Rate
```promql
rate(ai_tokens_consumed_total[5m])
```

### AI Completion P95 Latency
```promql
histogram_quantile(0.95, sum(rate(ai_completion_duration_ms_bucket[5m])) by (le))
```

### Health Check Status
Map `/health/ready` HTTP status code: 200 = Ready, 503 = Not Ready.

---

## 6. Alerting Recommendations

| Alert                         | Condition                                            | Severity |
|-------------------------------|------------------------------------------------------|----------|
| High HTTP error rate          | >5% of requests return 5xx over 5 minutes            | Critical |
| High AI latency               | p95 `ai.completion.duration_ms` > 10,000 ms          | Warning  |
| High AI cost                  | Hourly cost > $5 USD                                 | Warning  |
| Search unavailable            | `/health/ready` returns `Unhealthy` for azure-search  | Critical |
| Database unavailable          | `/health/ready` returns `Unhealthy` for database      | Critical |
| AI provider not configured    | `/health/ready` returns `Degraded` for ai-provider    | Info     |
