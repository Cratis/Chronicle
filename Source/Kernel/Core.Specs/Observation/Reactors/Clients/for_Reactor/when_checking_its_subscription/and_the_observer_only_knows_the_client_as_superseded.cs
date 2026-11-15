// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_checking_its_subscription;

/// <summary>
/// A superseded registration receives nothing until it is promoted, and a failed promotion leaves it unsubscribed, so it
/// is not confirmed intact and its keeper keeps checking it every revise interval.
/// </summary>
public class and_the_observer_only_knows_the_client_as_superseded : given.a_reactor_grain_with_a_stored_definition
{
    bool _intact;

    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(true);
        _observer.IsClientTarget(_client.ConnectionId).Returns(false);
    }

    async Task Because() => _intact = await _grain.IsSubscriptionIntact();

    [Fact] void should_not_be_intact() => _intact.ShouldBeFalse();
}
