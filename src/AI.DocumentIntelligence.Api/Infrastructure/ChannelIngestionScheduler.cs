using System.Threading.Channels;
using AI.DocumentIntelligence.Application.Abstractions.Ingestion;

namespace AI.DocumentIntelligence.Api.Infrastructure;

/// <summary>
/// Bounded in-process ingestion backlog backed by a <see cref="Channel{T}"/>. Producers
/// (the upload handler) wait when the backlog is full, applying natural back-pressure
/// instead of unbounded memory growth. Consumed by <see cref="IngestionWorker"/>.
///
/// In-process by design: jobs are lost on an unclean shutdown, which the worker
/// compensates for at startup by failing documents stuck in <c>Processing</c>.
/// Swap this implementation for a durable queue (Azure Storage Queue / Service Bus)
/// behind the same <see cref="IIngestionScheduler"/> interface when multi-instance
/// ingestion is needed — no caller changes required.
/// </summary>
internal sealed class ChannelIngestionScheduler : IIngestionScheduler
{
    // 256 pending documents is far beyond the UI's 4-document batches; the bound exists
    // to convert a runaway producer into back-pressure rather than memory exhaustion.
    private const int Capacity = 256;

    private readonly Channel<IngestionJob> _channel =
        Channel.CreateBounded<IngestionJob>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

    /// <summary>The consumer side, read exclusively by <see cref="IngestionWorker"/>.</summary>
    public ChannelReader<IngestionJob> Reader => _channel.Reader;

    /// <inheritdoc />
    public ValueTask ScheduleAsync(IngestionJob job, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(job, cancellationToken);
}
