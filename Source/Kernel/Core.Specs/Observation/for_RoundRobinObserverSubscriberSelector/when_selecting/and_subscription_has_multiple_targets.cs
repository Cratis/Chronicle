// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_RoundRobinObserverSubscriberSelector.when_selecting;

public class and_subscription_has_multiple_targets : Specification
{
    RoundRobinObserverSubscriberSelector _selector;
    ObserverSubscription _subscription;
    Key[] _partitions;
    ObserverSubscriberTarget[] _firstRound;
    ObserverSubscriberTarget[] _secondRound;

    void Establish()
    {
        _selector = new RoundRobinObserverSubscriberSelector();
        _subscription = new ObserverSubscription(
            "9bc2a145-3f4b-4a35-8a58-e6a8a4a0f6dd",
            ObserverKey.NotSet,
            [],
            typeof(NullObserverSubscriber),
            SiloAddress.Zero)
        {
            Targets =
            [
                new(SiloAddress.FromParsableString("127.0.0.1:11111@1"), new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" }),
                new(SiloAddress.FromParsableString("127.0.0.1:11112@1"), new ConnectedClient { ConnectionId = "b-connection", Version = "1.0.0" })
            ]
        };
        _partitions = [.. Enumerable.Range(0, 50).Select(index => (Key)$"partition-{index}")];
    }

    void Because()
    {
        _firstRound = [.. _partitions.Select(partition => _selector.Select(_subscription, partition))];
        _secondRound = [.. _partitions.Select(partition => _selector.Select(_subscription, partition))];
    }

    [Fact] void should_select_the_same_target_for_a_partition_every_time() => _firstRound.ShouldEqual(_secondRound);
    [Fact] void should_spread_partitions_across_all_targets() => _firstRound.Distinct().Count().ShouldEqual(2);
}
