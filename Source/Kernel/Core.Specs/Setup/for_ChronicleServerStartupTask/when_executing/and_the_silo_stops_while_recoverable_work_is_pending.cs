// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

public class and_the_silo_stops_while_recoverable_work_is_pending : given.a_startup_task_with_a_persisted_reactor
{
    readonly TaskCompletionSource _neverActivates = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _error;

    void Establish() => _reactorObserver.Ensure().Returns(_ => _neverActivates.Task);

    async Task Because()
    {
        await Execute();
        _error = await Catch.Exception(() => Stop().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_stop_without_an_exception() => _error.ShouldBeNull();
    [Fact] void should_have_ended_the_background_work() => RecoverableWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_not_have_faulted_the_background_work() => RecoverableWork.IsFaulted.ShouldBeFalse();
}
