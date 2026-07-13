// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_RandomLoadBalancerStrategy;

public class when_getting_next : Specification
{
    RandomLoadBalancerStrategy _strategy;
    ChronicleServerAddress[] _addresses;
    ChronicleServerAddress _selected;

    void Establish()
    {
        _strategy = new RandomLoadBalancerStrategy();
        _addresses =
        [
            new ChronicleServerAddress("host1"),
            new ChronicleServerAddress("host2"),
            new ChronicleServerAddress("host3")
        ];
    }

    void Because() => _selected = _strategy.Next(_addresses);

    [Fact] void should_select_one_of_the_addresses() => _addresses.ShouldContain(_selected);
}
