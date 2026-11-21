// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Resuming persisted jobs calls grains that may sit on a busy sibling. It waits only for its budget; readiness
/// does not wait past it, and the background work resumes the jobs once the sibling answers.
/// </summary>
public class and_resuming_jobs_outlasts_its_budget : given.a_startup_task
{
    readonly given.a_manual_time_provider _timeProvider = new();
    readonly TaskCompletionSource _rehydrating = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _recovery = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _calls;
    Exception _error;

    protected override TimeSpan? JobRecoveryBudget => TimeSpan.FromMilliseconds(100);

    protected override TimeProvider GetTimeProvider() => _timeProvider;

    void Establish() => _jobsManager.Rehydrate().Returns(_ =>
    {
        if (Interlocked.Increment(ref _calls) != 1) return Task.CompletedTask;
        _rehydrating.SetResult();
        return _recovery.Task;
    });

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        try
        {
            var execute = Execute();
            await _rehydrating.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
            _timeProvider.Expire();
            await execute.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        }
        finally
        {
            _recovery.SetResult();
        }
    });

    [Fact] void should_not_fail_startup() => _error.ShouldBeNull();
    [Fact] void should_report_the_job_recovery_as_deferred() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.JobRecoveryDeferred)).ShouldBeTrue();
    [Fact] void should_resume_the_jobs_in_the_background() => _calls.ShouldEqual(2);
    [Fact] void should_complete_startup() => _bootstrapClientsEnsured.ShouldBeTrue();
}
