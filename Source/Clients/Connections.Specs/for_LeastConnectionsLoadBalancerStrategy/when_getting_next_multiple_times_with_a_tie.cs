// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_LeastConnectionsLoadBalancerStrategy;

public class when_getting_next_multiple_times_with_a_tie : Specification
{
    LeastConnectionsLoadBalancerStrategy _strategy;
    ChronicleServerAddress[] _addresses;
    HashSet<ChronicleServerAddress> _selected;

    void Establish()
    {
        _addresses =
        [
            new ChronicleServerAddress("host1"),
            new ChronicleServerAddress("host2")
        ];
        var handler = new FakeConnectionCountHttpMessageHandler(new Dictionary<string, int> { ["host1"] = 0, ["host2"] = 0 });
        _strategy = new LeastConnectionsLoadBalancerStrategy(skipTlsValidation: true, new HttpClient(handler), maxSelectionJitter: TimeSpan.Zero);
    }

    void Destroy() => _strategy.Dispose();

    async Task Because()
    {
        _selected = [];
        for (var attempt = 0; attempt < 50; attempt++)
        {
            _selected.Add(await _strategy.Next(_addresses));
        }
    }

    [Fact] void should_pick_both_tied_servers_across_repeated_selections() => _selected.Count.ShouldEqual(2);
}
