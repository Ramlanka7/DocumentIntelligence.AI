using System.Text;
using AI.DocumentIntelligence.Application.Contracts;
using AI.DocumentIntelligence.Application.Contracts.Analysis;
using AI.DocumentIntelligence.Application.Contracts.Comparison;
using AI.DocumentIntelligence.Application.Contracts.Export;

namespace AI.DocumentIntelligence.Infrastructure.Export.Formatters;

/// <summary>
/// Exports analysis and comparison results as GitHub-flavoured Markdown (.md).
/// All citations are included as inline references and a numbered Sources section.
/// No external dependencies — the output is built with a <see cref="StringBuilder"/>.
/// </summary>
internal sealed class MarkdownExportFormatter : IExportFormatter
{
    public ExportFormat Format => ExportFormat.Markdown;

    public ExportDocumentResult FormatAnalysis(AnalysisResult result, string title)
    {
        var sb = new StringBuilder();

        AppendTitle(sb, title);
        AppendLine(sb, "*Document Intelligence Analysis Report*");
        AppendLine(sb, $"*Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC*");
        AppendLine(sb);
        AppendHRule(sb);

        AppendH2(sb, "Executive Summary");
        AppendLine(sb, result.ExecutiveSummary);
        AppendLine(sb);

        if (result.KeyFindings.Count > 0)
        {
            AppendH2(sb, "Key Findings");
            foreach (var finding in result.KeyFindings)
            {
                AppendH3(sb, finding.Title);
                AppendLine(sb, finding.Detail);
                AppendCitations(sb, finding.Citations);
                AppendLine(sb);
            }
        }

        if (result.Risks.Count > 0)
        {
            AppendH2(sb, "Risks");
            foreach (var risk in result.Risks)
            {
                AppendH3(sb, $"{risk.Title} — *Severity: {risk.Severity}*");
                AppendLine(sb, risk.Description);
                AppendCitations(sb, risk.Citations);
                AppendLine(sb);
            }
        }

        if (result.Recommendations.Count > 0)
        {
            AppendH2(sb, "Recommendations");
            foreach (var rec in result.Recommendations)
            {
                AppendH3(sb, rec.Title);
                AppendLine(sb, rec.Detail);
                AppendCitations(sb, rec.Citations);
                AppendLine(sb);
            }
        }

        if (result.ActionItems.Count > 0)
        {
            AppendH2(sb, "Action Items");
            foreach (var item in result.ActionItems)
            {
                sb.Append("- ");
                sb.AppendLine(item.Description);
                if (!string.IsNullOrWhiteSpace(item.Owner))
                {
                    sb.AppendLine($"  *Owner: {item.Owner}*");
                }

                AppendCitations(sb, item.Citations);
            }

            AppendLine(sb);
        }

        AppendSources(sb, result.Sources);

        return BuildResult(sb, title, "analysis");
    }

    public ExportDocumentResult FormatComparison(ComparisonResult result, string title)
    {
        var sb = new StringBuilder();

        AppendTitle(sb, title);
        AppendLine(sb, "*Document Intelligence Comparison Report*");
        AppendLine(sb, $"*Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC*");
        AppendLine(sb);
        AppendHRule(sb);

        AppendH2(sb, "Executive Overview");
        AppendLine(sb, result.ExecutiveOverview);
        AppendLine(sb);

        if (result.Differences.Count > 0)
        {
            AppendH2(sb, "Change Log");
            foreach (var diff in result.Differences)
            {
                AppendH3(sb, $"[{diff.Type}] {diff.Section}");
                AppendLine(sb, diff.Summary);
                if (!string.IsNullOrWhiteSpace(diff.Before))
                {
                    sb.AppendLine("**Before:**");
                    sb.AppendLine($"> {diff.Before}");
                    AppendLine(sb);
                }

                if (!string.IsNullOrWhiteSpace(diff.After))
                {
                    sb.AppendLine("**After:**");
                    sb.AppendLine($"> {diff.After}");
                    AppendLine(sb);
                }

                AppendCitations(sb, diff.Citations);
                AppendLine(sb);
            }
        }

        if (result.Risks.Count > 0)
        {
            AppendH2(sb, "Risks");
            foreach (var risk in result.Risks)
            {
                AppendH3(sb, $"{risk.Title} — *Severity: {risk.Severity}*");
                AppendLine(sb, risk.Description);
                AppendCitations(sb, risk.Citations);
                AppendLine(sb);
            }
        }

        if (result.Recommendations.Count > 0)
        {
            AppendH2(sb, "Recommendations");
            foreach (var rec in result.Recommendations)
            {
                AppendH3(sb, rec.Title);
                AppendLine(sb, rec.Detail);
                AppendCitations(sb, rec.Citations);
                AppendLine(sb);
            }
        }

        AppendSources(sb, result.Sources);

        return BuildResult(sb, title, "comparison");
    }

    // ---- helpers ---------------------------------------------------------------------------

    private static void AppendTitle(StringBuilder sb, string title)
    {
        sb.AppendLine($"# {title}");
        sb.AppendLine();
    }

    private static void AppendH2(StringBuilder sb, string heading)
    {
        sb.AppendLine($"## {heading}");
        sb.AppendLine();
    }

    private static void AppendH3(StringBuilder sb, string heading)
    {
        sb.AppendLine($"### {heading}");
        sb.AppendLine();
    }

    private static void AppendLine(StringBuilder sb, string text = "")
    {
        sb.AppendLine(text);
    }

    private static void AppendHRule(StringBuilder sb)
    {
        sb.AppendLine("---");
        sb.AppendLine();
    }

    private static void AppendCitations(StringBuilder sb, IReadOnlyList<Citation> citations)
    {
        if (citations.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("**Citations:**");
        foreach (var c in citations)
        {
            sb.AppendLine(
                $"- *{c.DocumentName}*, p.{c.PageNumber}, {c.ParagraphReference} " +
                $"(confidence: {c.ConfidenceScore:P0})");
            if (!string.IsNullOrWhiteSpace(c.Snippet))
            {
                sb.AppendLine($"  > \"{c.Snippet}\"");
            }
        }
    }

    private static void AppendSources(StringBuilder sb, IReadOnlyList<Citation> sources)
    {
        if (sources.Count == 0)
        {
            return;
        }

        AppendHRule(sb);
        AppendH2(sb, "Sources");
        for (var i = 0; i < sources.Count; i++)
        {
            var src = sources[i];
            sb.AppendLine(
                $"{i + 1}. **{src.DocumentName}**, p.{src.PageNumber}, {src.ParagraphReference} " +
                $"— confidence: {src.ConfidenceScore:P0}");
            if (!string.IsNullOrWhiteSpace(src.Snippet))
            {
                sb.AppendLine($"   > \"{src.Snippet}\"");
            }
        }
    }

    private static ExportDocumentResult BuildResult(StringBuilder sb, string title, string kind)
    {
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new ExportDocumentResult(
            bytes,
            "text/markdown; charset=utf-8",
            ExportFileNames.Generate(title, kind, "md"));
    }
}
