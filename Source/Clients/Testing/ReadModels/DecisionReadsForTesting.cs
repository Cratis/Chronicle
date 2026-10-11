// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>Folds in-process scenario events for protected decision reads.</summary>
/// <param name="eventStore">The in-process event store.</param>
/// <param name="admission">The same shape and key admission used by the .NET client.</param>
/// <param name="seeded">The seeded read models.</param>
internal sealed class DecisionReadsForTesting(EventStoreForTesting eventStore, DecisionReads admission, ReadModelsForTesting seeded) : IDecisionReads
{
    /// <inheritdoc/>
    public DecisionReadAdmission Admit<T>()
        where T : class => admission.Admit<T>();

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!eventStore.UnitOfWorkManager.HasCurrent)
        {
            throw new DecisionReadRequiresUnitOfWork();
        }
        var unitOfWork = eventStore.UnitOfWorkManager.Current;
        var read = await GetDetached<T>(key, cancellationToken);
        unitOfWork.AddDecisionRead(read);
        return read;
    }

    /// <inheritdoc/>
    public DecisionReadAdmission AdmitForStream<T>()
        where T : class => admission.AdmitForStream<T>();

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> Get<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!eventStore.UnitOfWorkManager.HasCurrent) throw new DecisionReadRequiresUnitOfWork();
        var unitOfWork = eventStore.UnitOfWorkManager.Current;
        var read = await GetDetached<T>(eventSourceId, eventStreamType, eventStreamId, eventSourceType, cancellationToken);
        unitOfWork.AddDecisionRead(read);
        return read;
    }

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class
    {
        var scope = new DecisionReadStreamScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType);
        return ReadDetached<T>(admission.GetStreamKey<T>(scope), scope, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class => ReadDetached<T>(key, null, cancellationToken);

    async Task<DecisionRead<T>> ReadDetached<T>(ReadModelKey key, DecisionReadStreamScope? streamScope, CancellationToken cancellationToken)
        where T : class
    {
        var (definition, types) = admission.GetAdmittedShape<T>(key, streamScope is not null);
        cancellationToken.ThrowIfCancellationRequested();
        var log = eventStore.EventLog;
        var boundary = await log.GetTailSequenceNumber();
        var events = streamScope is null
            ? await log.GetFromSequenceNumber(EventSequenceNumber.First, (EventSourceId)key, types)
            : await log.GetForEventSourceIdAndEventTypes(streamScope.EventSourceId, types, streamScope.EventStreamType, streamScope.EventStreamId, streamScope.EventSourceType);
        cancellationToken.ThrowIfCancellationRequested();
        T? instance;
        if (events.Count == 0)
        {
            // A seeded model has no corresponding stored event, but still guards against anything
            // appended after the current in-memory log boundary.
            instance = seeded.GetSeededInstance<T>(key);
        }
        else
        {
            var (_, instances) = await ProjectionReadModelProcessor.Process<T>(
                definition,
                events.Select(_ => (_.Context.EventSourceId, _.Content)),
                eventStore,
                eventStore.EventTypes,
                eventStore.JsonSchemaGenerator,
                eventContexts: events.Select(_ => _.Context).ToArray());
            instances.TryGetValue((EventSourceId)key, out instance);
        }
        var scopeBoundary = boundary.IsUnavailable ? EventSequenceNumber.BeforeFirst : boundary;
        return streamScope is null
            ? new DecisionRead<T>(key, instance, eventStore.Name, eventStore.Namespace, scopeBoundary, types)
            : new DecisionRead<T>(key, instance, eventStore.Name, eventStore.Namespace, scopeBoundary, types, streamScope);
    }
}
