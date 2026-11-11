// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks;
using Orleans.Runtime;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The registration settings and authority of a subscription operation.
    /// </summary>
    /// <param name="Type">The observer type.</param>
    /// <param name="EventTypes">The subscribed event types.</param>
    /// <param name="SiloAddress">The subscriber silo.</param>
    /// <param name="SubscriberArgs">The subscriber arguments.</param>
    /// <param name="IsReplayable">Whether replay is supported.</param>
    /// <param name="Filters">The subscription filters.</param>
    /// <param name="Additive">Whether event types are merged.</param>
    /// <param name="Recovering">Whether live progress is retained for recovery.</param>
    /// <param name="ReactivateRetired">Whether registration may reactivate a retired observer.</param>
    /// <param name="Automatic">Whether this is automatic reconciliation.</param>
    /// <param name="GenerationDelivery">The event generation delivery mode.</param>
    /// <param name="Reconcile">Whether setup must recheck reconciliation authority.</param>
    readonly record struct SubscriptionRequest(
        ObserverType Type,
        IEnumerable<EventType> EventTypes,
        SiloAddress SiloAddress,
        object? SubscriberArgs,
        bool IsReplayable,
        ObserverFilters? Filters,
        bool Additive = false,
        bool Recovering = false,
        bool ReactivateRetired = true,
        bool Automatic = false,
        EventGenerationDelivery GenerationDelivery = EventGenerationDelivery.Compatibility,
        bool Reconcile = false);

    /// <summary>
    /// The delivery settings applied to a subscriber or fan-out target.
    /// </summary>
    /// <param name="EventTypes">The subscribed event types.</param>
    /// <param name="SiloAddress">The subscriber silo.</param>
    /// <param name="SubscriberArgs">The subscriber arguments.</param>
    /// <param name="IsReplayable">Whether replay is supported.</param>
    /// <param name="Filters">The subscription filters.</param>
    /// <param name="GenerationDelivery">The event generation delivery mode.</param>
    readonly record struct SubscriptionDelivery(
        IEnumerable<EventType> EventTypes,
        SiloAddress SiloAddress,
        object? SubscriberArgs,
        bool IsReplayable,
        ObserverFilters? Filters,
        EventGenerationDelivery GenerationDelivery);

    /// <inheritdoc/>
    public Task Subscribe<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null,
        bool reactivateRetired = true,
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber
        => SubscribeWithGenerationDelivery<TObserverSubscriber>(type, eventTypes, siloAddress, EventGenerationDelivery.Compatibility, subscriberArgs, isReplayable, filters, reactivateRetired, automatic);

    /// <inheritdoc/>
    public Task SubscribeWithGenerationDelivery<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        EventGenerationDelivery generationDelivery,
        object? subscriberArgs = null,
        bool isReplayable = true,
        ObserverFilters? filters = null,
        bool reactivateRetired = true,
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber =>
        SubscribeToEventTypes<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, reactivateRetired: reactivateRetired, automatic: automatic, generationDelivery: generationDelivery);

    /// <inheritdoc/>
    public async Task SubscribeToAllEvents<TObserverSubscriber>(
        ObserverType type,
        SiloAddress siloAddress,
        object? subscriberArgs = null,
        bool isReplayable = true,
        bool reactivateRetired = true)
        where TObserverSubscriber : IObserverSubscriber
    {
        ThrowIfSealed();
        SupersedeRecoveryAfterQuarantine();
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        logger.Subscribing();
        logger.SubscribingToAllEvents();

        var storageWasReset = await ReloadObserverState();
        await observerDefinition.ReadStateAsync();
        await failures.ReadStateAsync();
        if (!reactivateRetired && State.AlertDisposition == AlertDisposition.Retired) return;
        await ClaimDefinitionForKey();

        // The failures were re-read from storage; publish them so the gauges never keep counts from before the read.
        PublishFailedPartitionsGauge();
        await ApplyPendingStartAtTail(storageWasReset);
        await BeginAlertLifecycle();
        await LeaveQuarantineForSubscription();

        _subscriptionSetupFailed = true;
        try
        {
            await SetUpAllEventsSubscription<TObserverSubscriber>(type, siloAddress, subscriberArgs, isReplayable);
            await RecoverAfterSubscribing(leavesQuarantine: true);
            _subscriptionSetupFailed = await GetCurrentState() is CatchingUpInFlight;
        }
        catch
        {
            _subscriptionSetupFailed = true;
            throw;
        }
    }

    async Task SubscribeToEventTypes<TObserverSubscriber>(SubscriptionRequest request)
        where TObserverSubscriber : IObserverSubscriber
    {
        var prepared = await PrepareSubscriptionRegistration<TObserverSubscriber>(request);
        if (prepared is not { } subscription) return;
        var leavesQuarantine = !subscription.Automatic && !subscription.Recovering;

        try
        {
            var eventTypeSchemas = await storage.GetEventStore(_observerKey.EventStore).EventTypes.GetFor(subscription.EventTypes);
            _eventTypeSchemas = eventTypeSchemas.ToDictionary(s => s.Type);

            // Another instance of a client already being observed for needs only to be added as a fan-out target.
            if (await TryHandOverSubscription<TObserverSubscriber>(subscription)) return;

            using var scope = logger.BeginObserverScope(_observerId, _observerKey);
            var storageWasReset = await ReloadSubscriptionState(subscription.Recovering);
            if (IsRetiredAndNotReactivating(subscription.ReactivateRetired)) return;

            // Only an accepted subscription may move the definition; persist ownership before any cleanup writes.
            await ClaimDefinitionForKey();

            // The failures may have been re-read from storage; publish them so the gauges never keep counts from before.
            PublishFailedPartitionsGauge();
            await ApplyPendingStartAtTail(storageWasReset);
            await BeginAlertLifecycle();
            var webhookSubscription = typeof(IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber));
            if (webhookSubscription)
            {
                await SetUpSubscription<TObserverSubscriber>(subscription);
                if (!await ValidateStoredWebhookTarget()) return;
            }

            await PrepareSubscriptionRecovery(subscription.Recovering, subscription.Automatic);
            _subscriptionSetupFailed = true;
            if (!webhookSubscription)
            {
                await SetUpSubscription<TObserverSubscriber>(subscription);
            }
            await RecoverAfterSubscribing(leavesQuarantine);

            // CatchingUpInFlight always schedules an onward transition on entry. Remaining there after
            // recovery returns means a failed entry write stranded it, and an ordinary retry could not re-enter.
            // Healthy asynchronous catch-up and deferred routing are separate from that setup failure.
            _subscriptionSetupFailed = await GetCurrentState() is CatchingUpInFlight;
        }
        catch
        {
            _subscriptionSetupFailed = true;
            throw;
        }
    }

    async Task<SubscriptionRequest?> PrepareSubscriptionRegistration<TObserverSubscriber>(SubscriptionRequest request)
        where TObserverSubscriber : IObserverSubscriber
    {
        request = request with { EventTypes = request.EventTypes.ToArray() };
        await ValidateGenerationDelivery(request.EventTypes, request.GenerationDelivery);
        if (!request.Automatic)
        {
            _recoverSubscriptionAfterQuarantine = false;
        }

        // Automatic kernel subscription cannot revive a retired observer or a sealed activation.
        // Ordinary Subscribe keeps its explicit-registration semantics, including throwing when sealed.
        if (IsAdditiveSubscriptionBlocked(request.Additive)) return null;
        ThrowIfSealed();
        await ThrowIfServedByAnotherTransport(typeof(TObserverSubscriber), request.SubscriberArgs);
        if (request.Recovering && !await NeedsSubscriptionRecovery(request.EventTypes)) return null;

        await SupersedeRecoveryForSubscription(request);
        return MergeSubscriptionRegistration(request);
    }

    async Task SupersedeRecoveryForSubscription(SubscriptionRequest request)
    {
        // Only a subscription that may end quarantine supersedes recovery owed by an earlier clearance.
        if (!request.Automatic && !request.Recovering)
        {
            await CancelWebhookProbe();
            SupersedeRecoveryAfterQuarantine();
        }
    }

    SubscriptionRequest MergeSubscriptionRegistration(SubscriptionRequest request)
    {
        if (request.Additive)
        {
            // Merge inside the serialized observer turn, retaining newer concurrent registrations.
            request = request with { EventTypes = MergeSubscribedEventTypes(request.EventTypes) };
        }
        return request;
    }

    async Task<bool> TryHandOverSubscription<TObserverSubscriber>(SubscriptionRequest request)
        where TObserverSubscriber : IObserverSubscriber =>
        !request.Additive && !request.Recovering && !request.Automatic && request.SubscriberArgs is ConnectedClient connectedClient &&
        await TryHandOverToAnotherClientInstance<TObserverSubscriber>(request.Type, request.EventTypes, request.SiloAddress, connectedClient, request.IsReplayable, request.Filters, request.GenerationDelivery);

    async Task<bool> ReloadSubscriptionState(bool recovering)
    {
        // Ordinary subscription and automatic reconciliation re-read storage, including after a shared
        // test silo's database reset, but must not let a stale snapshot overwrite an interleaved quarantine.
        // Recovery retains live progress rather than reloading a stale persisted position.
        var storageWasReset = false;
        if (!recovering)
        {
            var wasQuarantined = IsQuarantined;
            storageWasReset = await ReloadObserverState();

            await observerDefinition.ReadStateAsync();
            await failures.ReadStateAsync();
            if (ShouldDiscardQuarantineFromWipedWorld(storageWasReset, wasQuarantined))
            {
                // The quarantine belonged to a wiped world. Discard it only after all state has been reloaded,
                // so routing cannot act on stale definitions or failures from that world.
                DiscardQuarantineFromWipedWorld();
                await TransitionTo<Routing>();
            }
        }
        return storageWasReset;
    }

    bool ShouldDiscardQuarantineFromWipedWorld(bool storageWasReset, bool wasQuarantined) =>
        storageWasReset && wasQuarantined && IsQuarantined;

    async Task PrepareSubscriptionRecovery(bool recovering, bool automatic)
    {
        if (recovering)
        {
            // A failed entry write can strand the machine in CatchingUpInFlight, which cannot enter
            // itself. Leave it without resetting progress or granting authority to release quarantine.
            _subscriptionSetupFailed = true;
            await TransitionTo<Disconnected>();
        }
        else if (!automatic)
        {
            await LeaveQuarantineForSubscription();
        }
    }

    async Task SetUpSubscription<TObserverSubscriber>(SubscriptionRequest request)
        where TObserverSubscriber : IObserverSubscriber
    {
        if (request.Reconcile && CannotReconcileSubscription(typeof(IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;
        var owner = GetOwner<TObserverSubscriber>();
        logger.Subscribing();
        var eventTypeIds = request.EventTypes.Select(et => et.Id).ToArray();
        logger.SubscribingWithEventTypes(eventTypeIds.Length, string.Join(", ", eventTypeIds));

        observerDefinition.State = observerDefinition.State with
        {
            EventSequenceId = _observerKey.EventSequenceId,
            Type = request.Type,
            Owner = owner,
            EventTypes = request.EventTypes,
            IsReplayable = request.IsReplayable,
            GenerationDelivery = request.GenerationDelivery
        };
        await observerDefinition.WriteStateAsync();

        if (request.Reconcile && CannotReconcileSubscription(typeof(IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;
        ApplySubscription<TObserverSubscriber>(new SubscriptionDelivery(request.EventTypes, request.SiloAddress, request.SubscriberArgs, request.IsReplayable, request.Filters, request.GenerationDelivery));

        State = State with { SubscribesToAllEvents = false };
        await WriteStateAsync();
    }

    async Task SetUpAllEventsSubscription<TObserverSubscriber>(SubscriptionRequest request)
        where TObserverSubscriber : IObserverSubscriber
    {
        if (request.Reconcile && CannotReconcileSubscription(typeof(IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;
        observerDefinition.State = observerDefinition.State with
        {
            EventSequenceId = _observerKey.EventSequenceId,
            Type = request.Type,
            Owner = GetOwner<TObserverSubscriber>(),
            EventTypes = [],
            IsReplayable = request.IsReplayable
        };
        await observerDefinition.WriteStateAsync();

        if (request.Reconcile && CannotReconcileSubscription(typeof(IWebhookObserverSubscriber).IsAssignableFrom(typeof(TObserverSubscriber)))) return;
        _subscription = new(
            _observerId,
            _observerKey,
            [],
            typeof(TObserverSubscriber),
            request.SiloAddress,
            request.SubscriberArgs,
            request.IsReplayable);
        ForgetSupersededRegistrations();

        State = State with { SubscribesToAllEvents = true };
        await WriteStateAsync();
    }

    void ApplySubscription<TObserverSubscriber>(SubscriptionDelivery delivery)
        where TObserverSubscriber : IObserverSubscriber
    {
        var (eventTypes, siloAddress, subscriberArgs, isReplayable, filters, generationDelivery) = delivery;
        if (subscriberArgs is not ConnectedClient connectedClient)
        {
            ForgetSupersededRegistrations();
            _subscription = new(
                _observerId,
                _observerKey,
                eventTypes,
                typeof(TObserverSubscriber),
                siloAddress,
                subscriberArgs,
                isReplayable,
                filters)
            {
                GenerationDelivery = generationDelivery
            };
            return;
        }

        var target = new ObserverSubscriberTarget(siloAddress, connectedClient);
        if (CanFanOutInto<TObserverSubscriber>(eventTypes, filters, generationDelivery))
        {
            DropSupersededRegistration(connectedClient.ConnectionId);

            // Another instance of the same client is already subscribed with an identical
            // definition - add this instance as a fan-out target instead of replacing the
            // subscription. The stable ordering keeps partition selection deterministic.
            var targets = _subscription.Targets
                .Where(existing => existing.ConnectedClient!.ConnectionId != connectedClient.ConnectionId)
                .Append(target)
                .OrderBy(existing => existing.ConnectedClient!.ConnectionId.Value)
                .ToArray();
            _subscription = _subscription with
            {
                SiloAddress = targets[0].SiloAddress,
                Arguments = targets[0].ConnectedClient,
                Targets = targets
            };
            return;
        }

        // The registrations whose targets this replaces are kept, so the observer can hand over to one of them
        // when the client that now owns the subscription leaves.
        SupersedeTargetsOtherThan(connectedClient.ConnectionId);
        var observerType = observerDefinition.State.Type;
        _resubscribeTarget = existing => SubscribeToEventTypes<TObserverSubscriber>(
            observerType,
            eventTypes,
            existing.SiloAddress,
            existing.ConnectedClient,
            isReplayable,
            filters,
            reactivateRetired: false,
            automatic: true,
            generationDelivery: generationDelivery);

        _subscription = new(
            _observerId,
            _observerKey,
            eventTypes,
            typeof(TObserverSubscriber),
            siloAddress,
            subscriberArgs,
            isReplayable,
            filters)
        {
            Targets = [target],
            GenerationDelivery = generationDelivery
        };
    }

    static void ThrowIfPinsMultipleGenerations(IEnumerable<EventType> eventTypes)
    {
        var duplicate = eventTypes.GroupBy(_ => _.Id).FirstOrDefault(_ => _.Select(type => type.Generation).Distinct().Count() > 1);
        if (duplicate is not null)
        {
            throw new ObserverPinsMultipleGenerationsOfEventType(duplicate.Key);
        }
    }

    static bool FiltersAreEqual(ObserverFilters? left, ObserverFilters? right)
    {
        if (left is null || right is null)
        {
            return ReferenceEquals(left, right);
        }

        // ObserverFilters is a record, but its Tags collection makes the generated equality a
        // reference comparison - two identical registrations from different client instances
        // would never be considered equal. Compare structurally instead.
        return left.Tags.ToHashSet().SetEquals(right.Tags) &&
               Equals(left.EventSourceType, right.EventSourceType) &&
               Equals(left.EventStreamType, right.EventStreamType);
    }

    bool IsRetiredAndNotReactivating(bool reactivateRetired) => !reactivateRetired && State.AlertDisposition == AlertDisposition.Retired;

    bool IsAdditiveSubscriptionBlocked(bool additive) => additive && (_removed || IsRetired);

    void DiscardQuarantineFromWipedWorld()
    {
        // The entry time belongs to the discarded episode, not to a future quarantine.
        _quarantineRunningStateChangedAt = null;
        _webhookProbe = null;
        _armWebhookProbe = false;
        _recoverSubscriptionAfterQuarantine = false;
        _isPreparingCatchup = false;
        NotePreparationEnded();
        _catchupRecoveryAttempts = 0;
        _subscription = ObserverSubscription.Unsubscribed;
        ForgetSupersededRegistrations();
    }

    async Task ValidateGenerationDelivery(IEnumerable<EventType> eventTypes, EventGenerationDelivery generationDelivery)
    {
        if (generationDelivery != EventGenerationDelivery.Pinned) return;
        ThrowIfPinsMultipleGenerations(eventTypes);
        var eventTypesStorage = storage.GetEventStore(_observerKey.EventStore).EventTypes;
        foreach (var eventType in eventTypes)
        {
            if (!await eventTypesStorage.HasFor(eventType.Id, eventType.Generation))
            {
                throw new EventTypeGenerationNotRegisteredForObserver(eventType);
            }
        }
    }

    bool CanFanOutInto<TObserverSubscriber>(IEnumerable<EventType> eventTypes, ObserverFilters? filters, EventGenerationDelivery generationDelivery)
        where TObserverSubscriber : IObserverSubscriber =>
        _subscription.IsSubscribed &&
        _subscription.Targets.Count > 0 &&
        _subscription.SubscriberType == typeof(TObserverSubscriber) &&
        _subscription.GenerationDelivery == generationDelivery &&
        _subscription.EventTypes.ToHashSet().SetEquals(eventTypes) &&
        FiltersAreEqual(_subscription.Filters, filters);

    ObserverOwner GetOwner<TObserverSubscriber>()
        where TObserverSubscriber : IObserverSubscriber => typeof(TObserverSubscriber) switch
        {
            Type t when t.IsAssignableTo(typeof(IAmOwnedByClient)) => ObserverOwner.Client,
            Type t when t.IsAssignableTo(typeof(IAmOwnedByKernel)) => ObserverOwner.Kernel,
            _ => ObserverOwner.None
        };
}
