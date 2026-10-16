// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

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
    public Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class => GetFor<T>(ReadProtectionGuard.AmbientUnit(eventStore.UnitOfWorkManager, typeof(T).Name), key, cancellationToken);

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> GetFor<T>(IUnitOfWork unitOfWork, ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        ReadProtectionGuard.ProtectedRead(unitOfWork, typeof(T).Name);
        var read = await ReadDetached<T>(key, null, cancellationToken, unitOfWork.IsEnforcingReadProtection);
        unitOfWork.AddDecisionRead(read);
        return read;
    }

    /// <inheritdoc/>
    public DecisionReadAdmission AdmitForStream<T>()
        where T : class => admission.AdmitForStream<T>();

    /// <inheritdoc/>
    public Task<DecisionRead<T>> Get<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class => GetFor<T>(ReadProtectionGuard.AmbientUnit(eventStore.UnitOfWorkManager, typeof(T).Name), eventSourceId, eventStreamType, eventStreamId, eventSourceType, cancellationToken);

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> GetFor<T>(IUnitOfWork unitOfWork, EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class
    {
        ReadProtectionGuard.ProtectedRead(unitOfWork, typeof(T).Name);
        var scope = new DecisionReadStreamScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType);
        var read = await ReadDetached<T>(admission.GetStreamKey<T>(scope), scope, cancellationToken, unitOfWork.IsEnforcingReadProtection);
        unitOfWork.AddDecisionRead(read);
        return read;
    }

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, EventSourceType? eventSourceType = default, CancellationToken cancellationToken = default)
        where T : class
    {
        ReadProtectionGuard.Detached(nameof(GetDetached));
        var scope = new DecisionReadStreamScope(eventSourceId, eventStreamType, eventStreamId, eventSourceType);
        return ReadDetached<T>(admission.GetStreamKey<T>(scope), scope, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        ReadProtectionGuard.Detached(nameof(GetDetached));
        return ReadDetached<T>(key, null, cancellationToken);
    }

    /// <inheritdoc/>
    public DecisionReadDescription Describe<T>()
        where T : class => admission.Describe<T>();

    async Task<DecisionRead<T>> ReadDetached<T>(ReadModelKey key, DecisionReadStreamScope? streamScope, CancellationToken cancellationToken, bool enforcing = false)
        where T : class
    {
        if (eventStore.Reducers.HasFor<T>())
        {
            return await admission.ReadDetached<T>(key, streamScope, cancellationToken, enforcing);
        }
        var (definition, types) = admission.GetAdmittedShape<T>(key, streamScope is not null, enforcing);
        cancellationToken.ThrowIfCancellationRequested();
        var log = eventStore.EventLog;
        EventSequenceNumber boundary;
        IReadOnlyList<AppendedEvent> events;
        using (new ReadProtectionReadRegion(ReadAllowance.Contributed, "Decision fold transport"))
        {
            boundary = await log.GetTailSequenceNumber();
            events = streamScope is null
                ? await log.GetFromSequenceNumber(EventSequenceNumber.First, (EventSourceId)key, types)
                : await log.GetForEventSourceIdAndEventTypes(streamScope.EventSourceId, types, streamScope.EventStreamType, streamScope.EventStreamId, streamScope.EventSourceType);
        }
        cancellationToken.ThrowIfCancellationRequested();
        T? instance;
        if (events.Count == 0)
        {
            // A seeded model has no corresponding stored event, but still guards against anything
            // appended after the current in-memory log boundary.
            instance = EventStreamReadModel.DeclarationFor(typeof(T)) is null ? seeded.GetSeededInstance<T>(key) : null;
        }
        else
        {
            var (_, instances) = await ProjectionReadModelProcessor.Process<T>(
                definition,
                events.Select(_ => (_.Context.EventSourceId, _.Content)),
                new ReadModelFoldContext(eventStore, eventStore.EventTypes, eventStore.JsonSchemaGenerator, eventStore.ServiceProvider, typeof(T))
                {
                    EventContexts = events.Select(_ => _.Context).ToArray()
                });
            instances.TryGetValue((EventSourceId)key, out instance);
        }
        var scopeBoundary = boundary.IsUnavailable ? EventSequenceNumber.BeforeFirst : boundary;
        return streamScope is null
            ? new DecisionRead<T>(key, instance, eventStore.Name, eventStore.Namespace, scopeBoundary, types)
            : new DecisionRead<T>(key, instance, eventStore.Name, eventStore.Namespace, scopeBoundary, types, streamScope);
    }
}
