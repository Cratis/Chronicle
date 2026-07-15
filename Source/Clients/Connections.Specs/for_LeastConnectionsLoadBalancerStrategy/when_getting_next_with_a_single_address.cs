// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_getting_next_with_a_single_address : Specification
{
    LeastConnectionsLoadBalancerStrategy _strategy;
    ChronicleServerAddress _address;
    ChronicleServerAddress _selected;

    void Establish()
    {
        _address = new ChronicleServerAddress("host1");
        _strategy = new LeastConnectionsLoadBalancerStrategy(disableTls: true);
    }

    void Destroy() => _strategy.Dispose();

    async Task Because() => _selected = await _strategy.Next([_address]);

    [Fact] void should_return_the_only_address_without_probing() => _selected.ShouldEqual(_address);
}
