// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraints;

public class when_the_job_completes_successfully : given.a_reindex_job
{
    JobSummary? _summary;

    async Task Because()
    {
        // An empty plan finalizes the real job inline, before the reader sees any running state.
        await _job.Start(new(EventSequenceId.Log, []));
        _summary = await ReadJobById();
    }

    [Fact] void should_not_clear_the_job_state() => _state.Stats.Clears.ShouldEqual(0);
    [Fact] void should_find_the_completed_job_by_id_after_finalization() => (_summary?.Status ?? JobStatus.None).ShouldEqual(JobStatus.CompletedSuccessfully);
}
