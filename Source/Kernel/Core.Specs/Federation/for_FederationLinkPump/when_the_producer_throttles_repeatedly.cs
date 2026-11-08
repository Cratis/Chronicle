// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_the_producer_throttles_repeatedly : given.a_link_pump
{
    List<TimeSpan?> _delays;

    void Establish()
    {
        _delays = [];
        for (var attempt = 0; attempt < 10; attempt++)
        {
            _pull.Enqueue(given.FakePull.WithStatus(FederatedEventBatchStatus.Throttled));
        }
    }

    async Task Because()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            _delays.Add(await _pump.Step(default));
        }
    }

    [Fact] void should_start_at_the_initial_backoff() => _delays[0].ShouldEqual(_timing.InitialBackoff);
    [Fact] void should_double_the_backoff() => _delays[1].ShouldEqual(_timing.InitialBackoff * 2);
    [Fact] void should_never_exceed_the_cap() => _delays.All(_ => _ <= _timing.MaxBackoff).ShouldBeTrue();
    [Fact] void should_reach_the_cap() => _delays[^1].ShouldEqual(_timing.MaxBackoff);
    [Fact] void should_be_in_backoff() => _status.GetBinding(BindingId).State.ShouldEqual(FederationBindingStates.Backoff);
}
