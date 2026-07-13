// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_RoundRobinLoadBalancerStrategy;

public class when_getting_next_multiple_times : Specification
{
    RoundRobinLoadBalancerStrategy _strategy;
    ChronicleServerAddress[] _addresses;
    ChronicleServerAddress[] _selected;

    void Establish()
    {
        _strategy = new RoundRobinLoadBalancerStrategy(0);
        _addresses =
        [
            new ChronicleServerAddress("host1"),
            new ChronicleServerAddress("host2"),
            new ChronicleServerAddress("host3")
        ];
    }

    void Because() => _selected = [.. Enumerable.Range(0, 4).Select(_ => _strategy.Next(_addresses))];

    [Fact] void should_cycle_through_all_addresses_and_wrap_around() => _selected.ShouldEqual(_addresses[0], _addresses[1], _addresses[2], _addresses[0]);
}
