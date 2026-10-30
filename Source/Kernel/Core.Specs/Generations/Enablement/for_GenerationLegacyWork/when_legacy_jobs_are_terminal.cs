// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationLegacyWork;

public class when_legacy_jobs_are_terminal : given.legacy_work
{
    void Establish()
    {
        _replays = [new JobState { Status = JobStatus.CompletedSuccessfully }];
        _catchups = [new JobState { Status = JobStatus.CompletedWithFailures }];
    }

    async Task Because() => _blockers = await _reader.Inspect(new("orders"));

    [Fact] void should_accept_terminal_jobs() => _blockers.ShouldBeEmpty();
    [Fact] void should_not_change_replay_contexts() => _namespace.ReplayContexts.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetAll").ShouldBeTrue();
    [Fact] void should_not_change_jobs() => _namespace.Jobs.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetJobs").ShouldBeTrue();
}
