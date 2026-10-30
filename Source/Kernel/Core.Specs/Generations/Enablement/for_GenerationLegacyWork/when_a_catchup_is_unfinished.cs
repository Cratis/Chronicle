// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationLegacyWork;

public class when_a_catchup_is_unfinished : given.legacy_work
{
    void Establish()
    {
        _catchups = [new JobState { Id = new(Guid.NewGuid()), Status = JobStatus.Stopped }];
    }

    async Task Because() => _blockers = await _reader.Inspect(new("orders"));

    [Fact] void should_refuse_unfinished_catchup() => _blockers.Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.LegacyReplayJobUnresolved);
    [Fact] void should_not_change_replay_contexts() => _namespace.ReplayContexts.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetAll").ShouldBeTrue();
    [Fact] void should_not_change_jobs() => _namespace.Jobs.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetJobs").ShouldBeTrue();
}
