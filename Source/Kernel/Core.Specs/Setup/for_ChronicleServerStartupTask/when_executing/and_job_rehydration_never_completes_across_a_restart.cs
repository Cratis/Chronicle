// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Regression for #4040: outstanding job rehydration is backlog-proportional and must not keep the kernel from
/// becoming ready, neither on first start nor when it is started again against the same backlog (a restart).
/// </summary>
public class and_job_rehydration_never_completes_across_a_restart : given.a_startup_task_with_a_persisted_reactor
{
    readonly TaskCompletionSource _neverCompletes = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _readyAfterFirstStartup;
    Task _firstBackgroundPass = Task.CompletedTask;
    Exception _firstError;
    Exception _secondError;

    void Establish() => _jobsManager.Rehydrate().Returns(_ => _neverCompletes.Task);

    async Task Because()
    {
        _firstError = await Catch.Exception(Execute);
        _readyAfterFirstStartup = _bootstrapClientsEnsured;
        _firstBackgroundPass = RecoverableWork;
        _bootstrapClientsEnsured = false;
        _secondError = await Catch.Exception(Execute);
    }

    void Destroy()
    {
        _neverCompletes.TrySetResult();
        Stop().GetAwaiter().GetResult();
    }

    [Fact] void should_complete_the_first_startup() => _firstError.ShouldBeNull();
    [Fact] void should_be_ready_after_the_first_startup() => _readyAfterFirstStartup.ShouldBeTrue();
    [Fact] void should_complete_the_second_startup() => _secondError.ShouldBeNull();
    [Fact] void should_be_ready_after_the_second_startup() => _bootstrapClientsEnsured.ShouldBeTrue();
    [Fact] void should_still_have_the_first_backlog_pass_pending() => _firstBackgroundPass.IsCompleted.ShouldBeFalse();
}
