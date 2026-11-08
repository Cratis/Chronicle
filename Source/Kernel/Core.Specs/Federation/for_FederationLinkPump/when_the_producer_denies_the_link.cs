// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_the_producer_denies_the_link : given.a_link_pump
{
    TimeSpan? _delay;

    void Establish() => _pull.Enqueue(given.FakePull.WithStatus(FederatedEventBatchStatus.Denied));

    async Task Because() => _delay = await _pump.Step(default);

    [Fact] void should_be_in_the_denied_state() => _status.GetBinding(BindingId).State.ShouldEqual(FederationBindingStates.Denied);
    [Fact] void should_wait_the_long_denied_backoff_before_trying_again() => _delay.ShouldEqual(_timing.DeniedBackoff);
    [Fact] void should_not_retry_in_a_tight_loop() => (_delay > _timing.MaxBackoff).ShouldBeTrue();
    [Fact] void should_close_the_connection() => _clients.Closed.ShouldEqual(1);
    [Fact] void should_not_append_anything() => _inbox.Appended.ShouldBeEmpty();
}
