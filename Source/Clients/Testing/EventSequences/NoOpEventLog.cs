// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Linq;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op <see cref="IEventLog"/> for the in-process kernel command pipeline.
/// </summary>
/// <remarks>
/// <c>AddCratisArcCore</c> discovers every <c>ICommandExecutionScope</c> across the whole process, not just the
/// ones a given service collection cares about - an Arc.Chronicle consumer's transactional command scope is
/// discovered here too, even though this pipeline only ever executes the kernel's own commands, which append
/// directly through the grain and never touch the client event log. Without a registration, that scope falls back
/// to auto-activating the real client <c>EventLog</c>, which needs a live connection this in-process kernel does
/// not have. This satisfies the resolution harmlessly instead - only <see cref="AppendOperations"/> is ever
/// actually read, to subscribe for immediate-append failures that never occur here.
/// </remarks>
internal sealed class NoOpEventLog : IEventLog
{
    /// <inheritdoc/>
    public EventSequenceId Id => EventSequenceId.Log;

    /// <inheritdoc/>
    public IObservable<IEnumerable<AppendedEventWithResult>> AppendOperations { get; } = Observable.Never<IEnumerable<AppendedEventWithResult>>();

    /// <inheritdoc/>
    public ITransactionalEventSequence Transactional => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> filterEventTypes, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default) =>
        throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<bool> HasEventsFor(EventSourceId eventSourceId) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetFromSequenceNumber(EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = default, IEnumerable<EventType>? filterEventTypes = default) =>
        throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSourceId? eventSourceId = default,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? filterEventTypes = default) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<AppendResult> Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        ConcurrencyScope? concurrencyScope = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        CorrelationId? correlationId = default,
        IEnumerable<string>? tags = default,
        IDictionary<EventSourceId, ConcurrencyScope>? concurrencyScopes = default) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task Revise(EventSequenceNumber sequenceNumber, object @event) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason reason) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason reason, params Type[] clrEventTypes) => throw new EventLogNotAvailableInKernelPipeline();

    /// <inheritdoc/>
    public Task<Result<EventSequenceNumber, CompleteStreamError>> CompleteStream(EventStreamType eventStreamType, EventStreamId eventStreamId) =>
        throw new EventLogNotAvailableInKernelPipeline();
}
