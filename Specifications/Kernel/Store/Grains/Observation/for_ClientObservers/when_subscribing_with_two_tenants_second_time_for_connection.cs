// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_ClientObservers;

public class when_subscribing_with_two_tenants_second_time_for_connection : given.two_tenants_and_two_event_types
{
    async Task Establish() => await observers.Subscribe(observer_id, event_sequence_id, event_types);

    async Task Because() => await observers.Subscribe(observer_id, event_sequence_id, event_types);

    [Fact] void should_subscribe_for_disconnected_client_only_once() => connected_clients.Verify(_ => _.SubscribeOnDisconnected(connection_id, observers), Once());
}
