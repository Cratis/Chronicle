// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_silo_leaves;

public class and_the_departure_source_does_not_renew : given.a_silo_supervisor
{
    ILifecycleObserver _lifecycle;

    async Task Establish()
    {
        _departures.Next.Returns(Task.CompletedTask);
        _lifecycle = await Start();
    }

    async Task Because()
    {
        await Task.Delay(200);
        await _lifecycle.OnStop(CancellationToken.None);
    }

    [Fact] void should_not_open_the_window() => _supervisor.IsDepartureWindowOpen.ShouldBeFalse();
}
