// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ClientObserversState;

public class when_disconnected_with_associated_observer : given.client_observers_state
{
    void Establish() => state.AssociateObserverWithConnectionId(connection_id, new(observer_id, new(microservice_id, tenant_id, event_sequence_id)));

    void Because() => state.Disconnected(connection_id);

    [Fact] void should_not_have_connection_id() => state.HasConnectionId(connection_id).ShouldBeFalse();
}
