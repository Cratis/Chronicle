// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.DevelopmentTools.for_EventStoreSubscriptionsReset;

public class when_resetting : Specification
{
    IGrainFactory _grainFactory;
    IStorage _storage;
    IEventStoreSubscriptionsManager _dormant;
    IEventStoreSubscriptionsManager _active;
    IEventStoreSubscriptionsManager _orphan;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStores().Returns([new EventStoreName("dormant"), new EventStoreName("active")]);
        var lookup = Substitute.For<IEventStoreSubscriptionsManager, IGrainBase>();
        var context = Substitute.For<IGrainContext>();
        context.GrainId.Returns(GrainId.Create("subscriptions", "lookup"));
        ((IGrainBase)lookup).GrainContext.Returns(context);
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>("reset-type-lookup").Returns(lookup);
        var management = Substitute.For<IManagementGrain>();
        _grainFactory.GetGrain<IManagementGrain>(0).Returns(management);
        management.GetDetailedGrainStatistics().Returns([
            new DetailedGrainStatistic { GrainId = GrainId.Create("subscriptions", "active") },
            new DetailedGrainStatistic { GrainId = GrainId.Create("subscriptions", "orphan") },
            new DetailedGrainStatistic { GrainId = GrainId.Create("unrelated", "other") }
        ]);
        _dormant = Manager("dormant");
        _active = Manager("active");
        _orphan = Manager("orphan");
    }

    async Task Because() => await EventStoreSubscriptionsReset.Reset(_grainFactory, _storage);

    IEventStoreSubscriptionsManager Manager(string name)
    {
        var manager = Substitute.For<IEventStoreSubscriptionsManager>();
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>(name).Returns(manager);
        manager.GetSubscriptionDefinitions().Returns([new EventStoreSubscriptionDefinition("one", "source", []), new EventStoreSubscriptionDefinition("two", "source", [])]);
        return manager;
    }

    [Fact] async Task should_remove_dormant_subscriptions() => await _dormant.Received(1).Remove(new EventStoreSubscriptionId("one"));
    [Fact] async Task should_remove_every_definition() => await _dormant.Received(1).Remove(new EventStoreSubscriptionId("two"));
    [Fact] async Task should_reset_an_owner_in_both_inventories_once() => await _active.Received(1).Remove(new EventStoreSubscriptionId("one"));
    [Fact] async Task should_reset_an_owner_missing_from_storage() => await _orphan.Received(1).Remove(new EventStoreSubscriptionId("one"));
    [Fact] void should_not_activate_unrelated_grains() => _grainFactory.DidNotReceive().GetGrain<IEventStoreSubscriptionsManager>("other");
}
#endif
