// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.Observation.for_ClientObserversState;

public class when_asking_if_has_connection_id_without_any_associations : given.client_observers_state
{
    [Fact] void should_have_connection_id() => state.HasConnectionId(connection_id).ShouldBeFalse();
}
