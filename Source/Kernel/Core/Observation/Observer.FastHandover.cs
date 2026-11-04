// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Hands a steadily observing observer over to another instance of the same client, in memory only.
    /// </summary>
    /// <param name="type">The registered <see cref="ObserverType"/>.</param>
    /// <param name="eventTypes">The registered event types.</param>
    /// <param name="siloAddress">The silo the client instance is connected to.</param>
    /// <param name="connectedClient">The client instance registering.</param>
    /// <param name="isReplayable">Whether the registration is replayable.</param>
    /// <param name="filters">The registered filters.</param>
    /// <param name="generationDelivery">The registered generation delivery.</param>
    /// <typeparam name="TObserverSubscriber">The subscriber type.</typeparam>
    /// <returns>True if the instance was added as a fan-out target; false if the full subscribe must run.</returns>
    /// <remarks>
    /// A rollout connects every replica of a client, and each one registers the same definition. When the observer is
    /// already observing that exact definition for another instance, the only change is one more fan-out target, so
    /// re-reading state, rewriting the definition and state, rotating the alert lifecycle and re-running recovery for
    /// every replica is pure storage load. Anything that is not that steady case runs the full subscribe unchanged.
    /// The checks are repeated after the last await and the target is added without awaiting in between, because
    /// interleaving calls may move the observer out of the steady state while this turn waits on storage.
    /// </remarks>
    Task<bool> TryHandOverToAnotherClientInstance<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        ConnectedClient connectedClient,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery)
        where TObserverSubscriber : IObserverSubscriber =>
        TryAddClientInstanceTarget<TObserverSubscriber>(
            type,
            eventTypes,
            siloAddress,
            connectedClient,
            isReplayable,
            filters,
            generationDelivery,
            () => _startAtTailWhenSubscribed = false);

    /// <summary>
    /// Re-adds another instance of an observed client in memory only, for the keep-alive restore.
    /// </summary>
    /// <param name="type">The registered <see cref="ObserverType"/>.</param>
    /// <param name="eventTypes">The registered event types.</param>
    /// <param name="siloAddress">The silo the client instance is connected to.</param>
    /// <param name="connectedClient">The client instance being restored.</param>
    /// <param name="isReplayable">Whether the registration is replayable.</param>
    /// <param name="filters">The registered filters.</param>
    /// <param name="generationDelivery">The registered generation delivery.</param>
    /// <param name="schemas">The event type schemas read for this restore.</param>
    /// <typeparam name="TObserverSubscriber">The subscriber type.</typeparam>
    /// <returns>True if the instance was added as a fan-out target; false if the full reconcile must run.</returns>
    /// <remarks>
    /// The restore keeps <c language="csharp">_startAtTailWhenSubscribed</c> untouched because the full reconcile never changes it, and it
    /// applies the schemas only on success, so a fallback leaves the observer exactly as the full reconcile finds it today.
    /// </remarks>
    Task<bool> TryRestoreAsAnotherClientInstance<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        ConnectedClient connectedClient,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery,
        IEnumerable<EventTypeSchema> schemas)
        where TObserverSubscriber : IObserverSubscriber =>
        TryAddClientInstanceTarget<TObserverSubscriber>(
            type,
            eventTypes,
            siloAddress,
            connectedClient,
            isReplayable,
            filters,
            generationDelivery,
            () => _eventTypeSchemas = schemas.ToDictionary(schema => schema.Type));

    async Task<bool> TryAddClientInstanceTarget<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        ConnectedClient connectedClient,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery,
        Action onAdded)
        where TObserverSubscriber : IObserverSubscriber
    {
        var registration = new HandoverRegistration(type, GetOwner<TObserverSubscriber>(), eventTypes, isReplayable, generationDelivery);
        if (!CanHandOverTo<TObserverSubscriber>(registration, filters)) return false;
        if (await GetCurrentState() is not Observing) return false;
        if (!await _appendedEventsQueues.IsSubscribed(_observerKey)) return false;
        if (!await StoredStateBelongsToThisLifecycle()) return false;
        await observerDefinition.ReadStateAsync();

        if (!CanHandOverTo<TObserverSubscriber>(registration, filters) || !StoredDefinitionMatches(registration)) return false;

        ApplySubscription<TObserverSubscriber>(eventTypes, siloAddress, connectedClient, isReplayable, filters, generationDelivery);
        onAdded();
        RecordedHandlingState();
        return true;
    }

    bool CanHandOverTo<TObserverSubscriber>(HandoverRegistration registration, ObserverFilters? filters)
        where TObserverSubscriber : IObserverSubscriber =>
        !typeof(IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)) &&
        CanFanOutInto<TObserverSubscriber>(registration.EventTypes, filters, registration.GenerationDelivery) &&
        _subscription.IsReplayable == registration.IsReplayable &&
        IsSteadilyObserving() &&
        HasNoPendingRecovery();

    bool IsSteadilyObserving() =>
        State.RunningState == ObserverRunningState.Active &&
        !State.SubscribesToAllEvents &&
        !State.IsReplaying &&
        State.CatchingUpPartitions.Count == 0 &&
        _alertDisposition == AlertDisposition.Active &&
        _alertLifecycleId != Guid.Empty &&
        _observerExists &&
        KeyOwnsDefinition &&
        !_removed &&
        !IsQuarantined &&
        !_subscriptionSetupFailed &&
        !_stateWritingSuspended;

    bool HasNoPendingRecovery() =>
        !_resumingQuarantine &&
        !_recoverSubscriptionAfterQuarantine &&
        !_retryRecoveryAfterQuarantine &&
        _owedRecoveryAttempts == 0 &&
        _recoveriesInProgress == 0 &&
        _catchUpHandoversInFlight == 0 &&
        !_isPreparingCatchup &&
        !IsBackingOffCatchUp &&
        !Failures.HasFailedPartitions &&
        !_failureStateWritePending;

    /// <summary>
    /// Checks that the stored observer state is the one this activation is observing with.
    /// </summary>
    /// <returns>True if the stored state carries this activation's alert lifecycle.</returns>
    /// <remarks>
    /// Every accepted subscription persists a new alert lifecycle, so stored state that no longer carries it belongs to
    /// another world - wiped, or replaced. Handing over would keep this activation's cursor from the old world and skip
    /// the history of the new one; the full subscribe reloads the state instead.
    /// </remarks>
    async Task<bool> StoredStateBelongsToThisLifecycle()
    {
        var stored = await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Observers.Get(_observerId);
        return stored.Identifier == _observerId && stored.AlertLifecycleId == _alertLifecycleId;
    }

    bool StoredDefinitionMatches(HandoverRegistration registration)
    {
        var stored = observerDefinition.State;
        return stored.Identifier == _observerId &&
            stored.EventSequenceId == _observerKey.EventSequenceId &&
            stored.Type == registration.Type &&
            stored.Owner == registration.Owner &&
            stored.IsReplayable == registration.IsReplayable &&
            stored.GenerationDelivery == registration.GenerationDelivery &&
            stored.EventTypes.ToHashSet().SetEquals(registration.EventTypes);
    }

    sealed record HandoverRegistration(
        ObserverType Type,
        ObserverOwner Owner,
        IEnumerable<EventType> EventTypes,
        bool IsReplayable,
        EventGenerationDelivery GenerationDelivery);
}
