// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.for_EventStore.when_registering_all;

public class and_keep_alive_is_skipped_with_a_projection_from_another_store : given.an_event_store_with_a_projection_that_cannot_be_built
{
    const string OtherStore = "other-store";

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        var connection = Substitute.For<IChronicleConnection, IConnectionKeepAlive>();
        connection.Lifecycle.Returns(_connectionLifecycle);
        ((IConnectionKeepAlive)connection).IsSkipped.Returns(true);
        SetAutoProperty("Connection", connection);

        var projections = Substitute.For<IProjections>();
        projections.GetExternalEventStoreSubscriptions().Returns([(OtherStore, [new EventTypeId("happened-elsewhere")])]);
        SetAutoProperty("Projections", projections);
    }

    async Task Because()
    {
        await _projections.Discover();
        await _eventStore.RegisterAll();
    }

    [Fact] void should_subscribe_to_the_other_store() => _eventStore.Subscriptions.Received(1).Subscribe(new EventStoreSubscriptionId(OtherStore), OtherStore, Arg.Any<Action<IEventStoreSubscriptionBuilder>>());
    [Fact] void should_not_register_reactors() => _eventStore.Reactors.DidNotReceive().Register();
}
