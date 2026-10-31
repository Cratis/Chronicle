// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationLegacyWork;

public class when_there_is_no_legacy_work : given.legacy_work
{
    async Task Because() => _blockers = await _reader.Inspect(new("orders"));

    [Fact] void should_reconcile() => _blockers.ShouldBeEmpty();
    [Fact] void should_not_change_replay_contexts() => _namespace.ReplayContexts.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetAll").ShouldBeTrue();
    [Fact] void should_not_change_jobs() => _namespace.Jobs.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetJobs").ShouldBeTrue();
}
