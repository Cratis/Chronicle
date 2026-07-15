// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_getting_next_reserves_the_selected_server : Specification
{
    LeastConnectionsLoadBalancerStrategy _strategy;
    FakeConnectionCountHttpMessageHandler _handler;
    ChronicleServerAddress[] _addresses;
    ChronicleServerAddress _selected;

    void Establish()
    {
        _addresses =
        [
            new ChronicleServerAddress("host1"),
            new ChronicleServerAddress("host2")
        ];
        _handler = new FakeConnectionCountHttpMessageHandler(new Dictionary<string, int> { ["host1"] = 5, ["host2"] = 2 });
        _strategy = new LeastConnectionsLoadBalancerStrategy(skipTlsValidation: true, new HttpClient(_handler), maxSelectionJitter: TimeSpan.Zero);
    }

    void Destroy() => _strategy.Dispose();

    async Task Because() => _selected = await _strategy.Next(_addresses);

    [Fact] void should_reserve_a_slot_on_the_selected_server() => _handler.ReservationsFor(_selected.Host).ShouldEqual(1);
    [Fact] void should_not_reserve_a_slot_on_the_other_server() => _handler.ReservationsFor(_addresses.First(address => address != _selected).Host).ShouldEqual(0);
}
