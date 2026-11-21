// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A job interrupted by a restart is resumed before the node reports ready, so a client that reconnects meets the
/// recovered job rather than racing it. The background work does not resume it a second time.
/// </summary>
public class and_persisted_jobs_exist : given.a_startup_task
{
    async Task Because()
    {
        await ExecuteStartup();
        await _startupTask.DeferredWork;
    }

    [Fact] async Task should_resume_the_jobs_exactly_once() => await _jobsManager.Received(1).Rehydrate();
    [Fact] void should_not_report_the_job_recovery_as_deferred() => LogEntries.Any(_ => _.Event == nameof(ChronicleServerStartupTaskLogging.JobRecoveryDeferred)).ShouldBeFalse();
    [Fact] void should_report_the_job_recovery_phase() => LogMessages.Any(_ => _.Contains("'JobRecovery'", StringComparison.Ordinal)).ShouldBeTrue();
}
