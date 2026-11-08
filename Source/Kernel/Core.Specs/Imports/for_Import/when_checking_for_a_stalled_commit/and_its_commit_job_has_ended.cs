// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_Import.when_checking_for_a_stalled_commit;

/// <summary>
/// The commit job ended without failing the import, and only a job for another import is running, so nothing is left to finish it.
/// </summary>
public class and_its_commit_job_has_ended : given.an_import
{
    bool _failed;

    async Task Establish()
    {
        await Begin();
        await StageChunk(0, 3);
        await _grain.Complete(1, 3);
        await _grain.CommitStarted(EventSequenceNumber.First);
        _commitJobs.Add(CommitJob(JobStatus.CompletedWithFailures));
        _commitJobs.Add(CommitJob(JobStatus.Running, ImportId.New()));
        _time.Now += TimeSpan.FromHours(25);
    }

    async Task Because() => _failed = await _grain.FailIfStalled();

    [Fact] void should_tell_whether_it_failed_it() => _failed.ShouldEqual(true);
    [Fact] void should_be_failed() => Record.State.ShouldEqual(ImportState.Failed);
}
