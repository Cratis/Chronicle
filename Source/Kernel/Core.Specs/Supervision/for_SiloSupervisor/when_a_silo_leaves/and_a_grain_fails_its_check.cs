// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_silo_leaves;

public class and_a_grain_fails_its_check : given.a_silo_supervisor
{
    bool _healthyAsked;

    void Establish()
    {
        _supervisor.Register("failing", null, null, () => Task.FromException(new TimeoutException()));
        _supervisor.Register("healthy", null, null, () =>
        {
            _healthyAsked = true;
            return Task.CompletedTask;
        });
    }

    async Task Because() => await _supervisor.RunDeparturePass(TimeSpan.Zero);

    [Fact] void should_still_ask_the_other_grains() => _healthyAsked.ShouldBeTrue();
}
