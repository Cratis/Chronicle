// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class and_nothing_was_lost : given.a_reducer_grain_with_a_stored_definition
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(true);

    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] Task should_not_record_the_observer() => _connectedClients.DidNotReceiveWithAnyArgs().RecordObserver(default!, default!);
}
