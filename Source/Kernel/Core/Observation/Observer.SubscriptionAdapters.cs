// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    Task SubscribeToEventTypes<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        bool additive = false,
        bool recovering = false,
        bool reactivateRetired = true,
        bool automatic = false,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility)
        where TObserverSubscriber : IObserverSubscriber =>
        SubscribeToEventTypes<TObserverSubscriber>(new(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, additive, recovering, reactivateRetired, automatic, generationDelivery));

    Task SetUpSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery,
        bool reconcile = false)
        where TObserverSubscriber : IObserverSubscriber =>
        SetUpSubscription<TObserverSubscriber>(new(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, GenerationDelivery: generationDelivery, Reconcile: reconcile));

    Task SetUpAllEventsSubscription<TObserverSubscriber>(
        ObserverType type,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        bool reconcile = false)
        where TObserverSubscriber : IObserverSubscriber =>
        SetUpAllEventsSubscription<TObserverSubscriber>(new(type, [], siloAddress, subscriberArgs, isReplayable, null, Reconcile: reconcile));

    void ApplySubscription<TObserverSubscriber>(
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs,
        bool isReplayable,
        ObserverFilters? filters,
        EventGenerationDelivery generationDelivery)
        where TObserverSubscriber : IObserverSubscriber =>
        ApplySubscription<TObserverSubscriber>(new(eventTypes, siloAddress, subscriberArgs, isReplayable, filters, generationDelivery));
}
