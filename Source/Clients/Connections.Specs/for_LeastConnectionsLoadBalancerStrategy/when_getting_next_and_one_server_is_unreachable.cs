// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_getting_next_and_one_server_is_unreachable : Specification
{
    LeastConnectionsLoadBalancerStrategy _strategy;
    ChronicleServerAddress[] _addresses;
    ChronicleServerAddress _selected;

    void Establish()
    {
        _addresses =
        [
            new ChronicleServerAddress("host1"),
            new ChronicleServerAddress("host2")
        ];
        var handler = new FakeConnectionCountHttpMessageHandler(
            new Dictionary<string, int> { ["host2"] = 3 },
            new HashSet<string> { "host1" });
        _strategy = new LeastConnectionsLoadBalancerStrategy(skipTlsValidation: true, new HttpClient(handler), maxSelectionJitter: TimeSpan.Zero);
    }

    void Destroy() => _strategy.Dispose();

    async Task Because() => _selected = await _strategy.Next(_addresses);

    [Fact] void should_select_the_reachable_server() => _selected.ShouldEqual(_addresses[1]);
}
