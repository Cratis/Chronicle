// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if CHRONICLE_ROLLING_DIAGNOSTICS
using Cratis.Chronicle.Contracts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Candidate-only structured metadata transport, entirely absent from shipping builds.
/// Stages: 1 open, 2 complete, 3 stream error, 4 stream cancel, 5 batch, 6 member, 7 advance,
/// 8 publish attempt, 9 publish returned (not remote ACK), 10 dispatch cancel, 11 dispatch fault.
/// </summary>
internal static partial class ReactorsDeliveryDiagnosticsLogging
{
    /// <summary>
    /// Emits bounded primitive metadata without allowing a diagnostic provider to affect dispatch.
    /// </summary>
    /// <param name="logger">The reactor logger.</param>
    /// <param name="stage">The numeric stage.</param>
    /// <param name="stream">The stream incarnation.</param>
    /// <param name="batch">The stream-local ordinal.</param>
    /// <param name="delivery">The wire delivery identifier.</param>
    /// <param name="sequence">The sequence or new local checkpoint.</param>
    /// <param name="previous">The old local checkpoint.</param>
    /// <param name="state">The numeric result or replay state.</param>
    /// <param name="count">The exact batch count.</param>
    /// <param name="partition">The GUID partition, otherwise empty.</param>
    /// <param name="type">The GUID event type, otherwise empty.</param>
    /// <param name="source">The GUID event source, otherwise empty.</param>
    /// <param name="correlation">The correlation identifier.</param>
    /// <param name="redacted">Flags for omitted non-GUID metadata.</param>
    internal static void Record(ILogger<Reactors> logger, int stage, Guid stream, long batch = 0, ulong delivery = 0, ulong sequence = ulong.MaxValue, ulong previous = ulong.MaxValue, int state = 0, int count = 0, Guid partition = default, Guid type = default, Guid source = default, Guid correlation = default, int redacted = 0)
    {
        try
        {
            RollingDelivery(logger, stage, stream, batch, delivery, sequence, previous, state, count, partition, type, source, correlation, redacted);
        }
        catch
        {
            // A diagnostic provider must never change dispatch, cancellation, or the original failure.
        }
    }

    /// <summary>
    /// Opens a stream incarnation with its initial local ordinal.
    /// </summary>
    /// <param name="logger">The reactor logger.</param>
    /// <returns>The stream token and initial batch ordinal.</returns>
    internal static (Guid Stream, long Batch) Open(ILogger<Reactors> logger)
    {
        var stream = Guid.NewGuid();
        Record(logger, 1, stream);
        return (stream, 0);
    }

    /// <summary>
    /// Allocates a stream-local ordinal and records its exact batch header.
    /// </summary>
    /// <param name="logger">The reactor logger.</param>
    /// <param name="stream">The stream incarnation.</param>
    /// <param name="ordinal">The stream-local ordinal counter.</param>
    /// <param name="events">The received batch.</param>
    /// <returns>The new batch ordinal.</returns>
    internal static long NextBatch(ILogger<Reactors> logger, Guid stream, ref long ordinal, EventsToObserve events)
    {
        var batch = Interlocked.Increment(ref ordinal);
        Record(logger, 5, stream, batch, events.DeliveryId, state: (int)events.ReplayState, count: events.Events.Count, partition: Identifier(events.Partition));
        return batch;
    }

    /// <summary>
    /// Records stream failure or cancellation without retaining the exception.
    /// </summary>
    /// <param name="logger">The reactor logger.</param>
    /// <param name="stream">The stream incarnation.</param>
    /// <param name="cancelled">Whether the existing classifier identified cancellation.</param>
    internal static void StreamError(ILogger<Reactors> logger, Guid stream, bool cancelled) => Record(logger, cancelled ? 4 : 3, stream);

    /// <summary>
    /// Records every offered member, including members not reached after a dispatch failure.
    /// </summary>
    /// <param name="logger">The reactor logger.</param>
    /// <param name="stream">The stream incarnation.</param>
    /// <param name="batch">The stream-local ordinal.</param>
    /// <param name="events">The offered batch. Content is never inspected.</param>
    internal static void Members(ILogger<Reactors> logger, Guid stream, long batch, EventsToObserve events)
    {
        foreach (var member in events.Events)
        {
            var partition = Identifier(events.Partition);
            var type = Identifier(member.Context.EventType.Id);
            var source = Identifier(member.Context.EventSourceId);
            var redacted = Redacted(partition, 4) | Redacted(type, 1) | Redacted(source, 2);
            Record(logger, 6, stream, batch, events.DeliveryId, member.Context.SequenceNumber, count: events.Events.Count, partition: partition, type: type, source: source, correlation: member.Context.CorrelationId, redacted: redacted);
        }
    }

    /// <summary>
    /// Carries stream/batch correlation through awaited callbacks using the logger's scoped transport.
    /// </summary>
    /// <param name="logger">The reactor logger.</param>
    /// <param name="stream">The stream incarnation.</param>
    /// <param name="batch">The stream-local ordinal.</param>
    /// <returns>A failure-isolated scope, when available.</returns>
    internal static IDisposable? BeginScope(ILogger<Reactors> logger, Guid stream, long batch)
    {
        try
        {
            return new DiagnosticScope(logger.BeginScope(new KeyValuePair<string, object>[] { new("RollingStream", stream), new("RollingBatch", batch) }));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Retains only bounded GUID identifiers; arbitrary names are explicitly redacted.
    /// </summary>
    /// <param name="value">The identifier.</param>
    /// <returns>The GUID or empty.</returns>
    internal static Guid Identifier(string? value) => value is { Length: <= 36 } && Guid.TryParse(value, out var id) ? id : Guid.Empty;

    static int Redacted(Guid value, int flag) => value == Guid.Empty ? flag : 0;

    [LoggerMessage(Level = LogLevel.Trace, Message = "RollingDelivery Stage={Stage} Stream={Stream} Batch={Batch} Delivery={Delivery} Sequence={Sequence} Previous={Previous} State={State} Count={Count} Partition={Partition} Type={Type} Source={Source} Correlation={Correlation} Redacted={Redacted}")]
    static partial void RollingDelivery(ILogger<Reactors> logger, int stage, Guid stream, long batch, ulong delivery, ulong sequence, ulong previous, int state, int count, Guid partition, Guid type, Guid source, Guid correlation, int redacted);

    sealed class DiagnosticScope(IDisposable? scope) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                scope?.Dispose();
            }
            catch
            {
                // Diagnostic teardown must not replace a dispatch failure.
            }
        }
    }
}
#endif
