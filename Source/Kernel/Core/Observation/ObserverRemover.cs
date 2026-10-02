// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverRemover"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching observer and manager grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for deleting the observer's records.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <remarks>
/// Removal is deliberately store-wide rather than per namespace. An observer's definition, and the projection
/// definition it has when it is a projection, are store-level records shared by every namespace; deleting those while
/// leaving the namespaced state behind elsewhere produces exactly the half-present observer the removal exists to get
/// rid of. The guard is evaluated in every namespace before sequential, nontransactional cleanup. A later failure
/// does not restore earlier namespaces or undo committed retirement. Concurrent explicit registration is not fenced
/// across namespaces or shared-definition cleanup.
/// </remarks>
public class ObserverRemover(
    IGrainFactory grainFactory,
    IStorage storage,
    ILogger<ObserverRemover> logger) : IObserverRemover
{
    /// <inheritdoc/>
    public async Task<ObserverRemovalResult> Remove(EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var namespaces = (await grainFactory.GetGrain<INamespaces>(eventStore).GetAll()).ToArray();

        var isRegistered = await eventStoreStorage.Observers.Has(observerId);
        if (!isRegistered && !await HasStateInAnyNamespace(eventStore, observerId, namespaces))
        {
            return ObserverRemovalResult.NotFound;
        }

        var guardResult = await EvaluateGuard(eventStore, observerId, eventSequenceId, namespaces);
        if (guardResult.Outcome != ObserverRemovalOutcome.Removed)
        {
            logger.RefusingObserverRemoval(observerId, guardResult.Outcome, guardResult.BlockingNamespace);
            return guardResult;
        }

        logger.RemovingObserver(observerId, eventStore);

        foreach (var @namespace in namespaces)
        {
            await grainFactory.GetGrain<IObserver>(new ObserverKey(observerId, eventStore, @namespace, eventSequenceId)).Remove();
        }

        await eventStoreStorage.Observers.Delete(observerId);
        await ForgetProjection(eventStore, observerId);

        logger.RemovedObserver(observerId, eventStore);
        return ObserverRemovalResult.Removed;
    }

    async Task<bool> HasStateInAnyNamespace(EventStoreName eventStore, ObserverId observerId, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var state = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers.Get(observerId);
            if (state.Identifier == observerId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Decides whether the observer may be removed.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the observer observes.</param>
    /// <param name="namespaces">Every namespace in the event store.</param>
    /// <returns>An <see cref="ObserverRemovalResult"/> that is <see cref="ObserverRemovalResult.Removed"/> when the guard passes.</returns>
    /// <remarks>
    /// Two separate questions, and both have to be asked. The running state says whether the observer is doing work;
    /// the subscription says whether a client is still reporting it, which is the real signal that its declaring code
    /// still exists. An observer can read as Disconnected in its stored state while a client has just resubscribed to
    /// it, so the stored state alone would let a live observer be torn out from under a running application.
    /// </remarks>
    async Task<ObserverRemovalResult> EvaluateGuard(
        EventStoreName eventStore,
        ObserverId observerId,
        EventSequenceId eventSequenceId,
        IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var observer = grainFactory.GetGrain<IObserver>(new ObserverKey(observerId, eventStore, @namespace, eventSequenceId));

            if (await observer.IsSubscribed())
            {
                return ObserverRemovalResult.Subscribed(@namespace);
            }

            var state = await observer.GetState();
            if (state.RunningState == ObserverRunningState.Active)
            {
                return ObserverRemovalResult.Active(@namespace);
            }
        }

        return ObserverRemovalResult.Removed;
    }

    /// <summary>
    /// Drops the projection definition an observer of type projection has, and tells the projections manager to forget it.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer being removed.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Deleting the stored definition alone is not enough: the manager keeps its own registered set, and its next
    /// activation subscribes everything in that set - recreating the observer records this removal just deleted.
    /// </remarks>
    async Task ForgetProjection(EventStoreName eventStore, ObserverId observerId)
    {
        var projectionId = (ProjectionId)observerId.Value;
        if (!await storage.GetEventStore(eventStore).Projections.Has(projectionId))
        {
            return;
        }

        await grainFactory.GetGrain<IProjectionsManager>(eventStore).Forget(projectionId);
        await storage.GetEventStore(eventStore).Projections.Delete(projectionId);
    }
}
