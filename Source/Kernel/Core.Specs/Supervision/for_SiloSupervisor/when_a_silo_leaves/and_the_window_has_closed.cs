// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_silo_leaves;

public class and_the_window_has_closed : given.a_silo_supervisor
{
    int _checks;

    void Establish()
    {
        _supervisor.Register("a", null, null, () =>
        {
            _checks++;
            return Task.CompletedTask;
        });
        _supervisor.OpenDepartureWindow();
        _clock.Advance(TimeSpan.FromSeconds(new Configuration.Clients().SubscriptionSettleSeconds + 1));
    }

    async Task Because() => await Task.WhenAll(_supervisor.Tick(CancellationToken.None));

    [Fact] void should_be_closed() => _supervisor.IsDepartureWindowOpen.ShouldBeFalse();
    [Fact] void should_not_start_another_pass() => _checks.ShouldEqual(0);
}
