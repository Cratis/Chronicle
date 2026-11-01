// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class and_the_observer_still_knows_the_client : given.a_reducer_grain_with_a_stored_definition
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(true);

    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] async Task should_check_with_the_observer() => await _observer.Received(1).KnowsClient(_client.ConnectionId);
    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
}
