// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_checking_its_subscription;

public class and_the_observer_knows_the_client : given.a_reducer_grain_with_a_stored_definition
{
    bool _intact;

    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(true);

    async Task Because() => _intact = await _grain.IsSubscriptionIntact();

    [Fact] void should_be_intact() => _intact.ShouldBeTrue();
    [Fact] async Task should_not_read_the_whole_subscription() => await _observer.DidNotReceive().GetSubscription();
}
