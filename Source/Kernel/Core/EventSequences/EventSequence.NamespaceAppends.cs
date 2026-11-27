// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence : IDisposable
{
    readonly CancellationTokenSource _namespaceActivationLifetime = new();
    NamespaceIncarnationId? _activationIncarnation;
    NamespaceBoundAppendStorage? _namespaceAppendStorage;

    /// <inheritdoc/>
    public Task<AppendResult> AppendToNamespace(NamespaceIncarnationId expectedIncarnation, EventToAppend @event, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScope concurrencyScope) =>
        InNamespace(expectedIncarnation, () => AppendValidated(
            @event.EventSourceType, @event.EventSourceId, @event.eventStreamType, @event.eventStreamId, @event.EventType, @event.Content, correlationId, causation, causedBy, @event.Tags, concurrencyScope, @event.Occurred, @event.Subject, @event.NamedTags, @event.EventSource));

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendManyToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes concurrencyScopes) =>
        InNamespace(expectedIncarnation, () => AppendMany(events, correlationId, causation, causedBy, concurrencyScopes));

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the activation's namespace commit lifetime.</summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        _namespaceActivationLifetime.Dispose();
        _metrics?.Dispose();
        _metrics = null;
        _spillRetryTimer?.Dispose();
        _spillRetryTimer = null;
        _constraintIndexRepairTimer?.Dispose();
        _constraintIndexRepairTimer = null;
    }

    async Task<T> InNamespace<T>(NamespaceIncarnationId expectedIncarnation, Func<Task<T>> append)
    {
        if (!_mayWriteSequence) throw new ReservedEventSequenceName(_eventSequenceId);
        var fenced = EventSequenceStorage as INamespaceFencedEventSequenceStorage ?? throw new NamespaceCommitNotSupported(new NotSupportedException());

        // The grain is non-reentrant. This typed binding is owned by this call, never caller ambient context.
        _namespaceAppendStorage = new(fenced, new(expectedIncarnation, _namespaceActivationLifetime.Token) { ActivationIncarnation = _activationIncarnation });
        try
        {
            return await append();
        }
        catch (NamespaceIncarnationRequiresReactivation)
        {
            DeactivateOnIdle();
            throw;
        }
        finally
        {
            _namespaceAppendStorage = null;
        }
    }

    Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> AppendSingleWithNamespaceFence(EventToAppendToStorage attempted, IReadOnlyCollection<NamedTag> namedTags, EventSourceName? eventSource, Func<Task<Result<AppendedEvent, DuplicateEventSequenceNumber>>> legacyAppend) =>
        _namespaceAppendStorage is { } bound
            ? bound.Append(attempted with { NamedTags = namedTags, EventSource = eventSource ?? EventSourceName.NotSet })
            : legacyAppend();

    async Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendBatchWithNamespaceFence(IReadOnlyList<EventToAppendToStorage> events, Func<Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>>> legacyAppend)
    {
        if (_namespaceAppendStorage is not { } bound) return await legacyAppend();
        var result = await bound.AppendMany(events);
        return result.TryGetError(out var failure)
            ? (Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>)failure.AsT0
            : Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(result.AsT0);
    }

    bool CanSettleAppendFailure(Exception error) =>
        _namespaceAppendStorage is null && error is not EventsNotCommitted and not EventPublicationConflict;

    void EndNamespaceActivation() => _namespaceActivationLifetime.Cancel();
}
