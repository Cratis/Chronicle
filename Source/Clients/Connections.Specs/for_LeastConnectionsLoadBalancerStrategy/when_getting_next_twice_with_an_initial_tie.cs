// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_getting_next_twice_with_an_initial_tie : Specification
{
    LeastConnectionsLoadBalancerStrategy _strategy;
    ChronicleServerAddress[] _addresses;
    ChronicleServerAddress _first;
    ChronicleServerAddress _second;

    void Establish()
    {
        _addresses =
        [
            new ChronicleServerAddress("host1"),
            new ChronicleServerAddress("host2")
        ];
        var handler = new FakeConnectionCountHttpMessageHandler(new Dictionary<string, int> { ["host1"] = 0, ["host2"] = 0 });
        _strategy = new LeastConnectionsLoadBalancerStrategy(disableTls: true, new HttpClient(handler), maxSelectionJitter: TimeSpan.Zero);
    }

    void Destroy() => _strategy.Dispose();

    async Task Because()
    {
        _first = await _strategy.Next(_addresses);
        _second = await _strategy.Next(_addresses);
    }

    [Fact] void should_pick_the_other_server_the_second_time() => _second.ShouldNotEqual(_first);
}
