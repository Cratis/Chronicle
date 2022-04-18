// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_ClientObservers;

public class when_subscribing_with_two_tenants_first_time_for_connection : given.two_tenants_and_two_event_types
{
    async Task Because() => await observers.Subscribe("My observer", observer_id, event_sequence_id, event_types);

    [Fact] void should_subscribe_for_disconnected_client() => connected_clients.Verify(_ => _.SubscribeOnDisconnected(connection_id, observers), Once());
    [Fact] void should_subscribe_observer_for_first_tenant() => first_tenant_observer.Verify(_ => _.Subscribe(event_types, connection_id), Once());
    [Fact] void should_subscribe_observer_for_second_tenant() => second_tenant_observer.Verify(_ => _.Subscribe(event_types, connection_id), Once());
    [Fact] void should_have_associated_connection_id() => state_on_write.HasConnectionId(connection_id).ShouldBeTrue();
    [Fact] void should_hold_observer_associated_with_connection_id() => state_on_write.GetObserversForConnectionId(connection_id).First().ObserverId.ShouldEqual(observer_id);
    [Fact] void should_hold_observer_with_correct_microservice_in_key_associated_with_connection_id() => state_on_write.GetObserversForConnectionId(connection_id).First().ObserverKey.MicroserviceId.ShouldEqual(execution_context.MicroserviceId);
    [Fact] void should_hold_observer_with_correct_tenant_in_key_associated_with_connection_id() => state_on_write.GetObserversForConnectionId(connection_id).First().ObserverKey.TenantId.ShouldEqual(execution_context.TenantId);
    [Fact] void should_hold_observer_with_correct_event_sequence_in_key_associated_with_connection_id() => state_on_write.GetObserversForConnectionId(connection_id).First().ObserverKey.EventSequenceId.ShouldEqual(event_sequence_id);
}
