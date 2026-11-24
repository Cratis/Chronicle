// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision.for_SiloSupervisor.when_a_silo_leaves;

public class and_the_cluster_reports_a_departure : given.a_silo_supervisor
{
    readonly TaskCompletionSource _departure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ILifecycleObserver _lifecycle;
    bool _openBefore;

    async Task Establish()
    {
        _departures.Next.Returns(_departure.Task, new TaskCompletionSource().Task);
        _lifecycle = await Start();
        _openBefore = _supervisor.IsDepartureWindowOpen;
    }

    async Task Because()
    {
        _departure.SetResult();
        await WaitUntil(() => _supervisor.IsDepartureWindowOpen);
        await _lifecycle.OnStop(CancellationToken.None);
    }

    [Fact] void should_not_be_open_before_a_silo_leaves() => _openBefore.ShouldBeFalse();
    [Fact] void should_open_the_window() => _supervisor.IsDepartureWindowOpen.ShouldBeTrue();
}
