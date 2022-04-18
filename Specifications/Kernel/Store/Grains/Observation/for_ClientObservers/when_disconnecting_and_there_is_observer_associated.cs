// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_ClientObservers;

public class when_disconnecting_and_there_is_observer_associated : given.two_tenants_and_two_event_types
{
    async Task Establish()
    {
        await observers.Subscribe("My observer", observer_id, event_sequence_id, event_types);
        storage.Invocations.Clear();
    }

    void Because() => observers.Disconnected(connection_id);

    [Fact] void should_unsubscribe_observer_for_first_tenant() => first_tenant_observer.Verify(_ => _.Unsubscribe(), Once());
    [Fact] void should_unsubscribe_observer_for_second_tenant() => second_tenant_observer.Verify(_ => _.Unsubscribe(), Once());
    [Fact] void should_disconnect_in_state() => state_on_write.HasConnectionId(connection_id).ShouldBeFalse();
}
