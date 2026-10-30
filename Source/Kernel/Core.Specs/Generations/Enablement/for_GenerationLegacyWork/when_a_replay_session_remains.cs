// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationLegacyWork;

public class when_a_replay_session_remains : given.legacy_work
{
    void Establish()
    {
        _contexts = [new(new("orders", new(1)), "orders-replay", "orders-revert", default)];
    }

    async Task Because() => _blockers = await _reader.Inspect(new("orders"));

    [Fact] void should_refuse_ambiguous_session() => _blockers.Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.LegacyReplaySessionUnresolved);
    [Fact] void should_not_change_replay_contexts() => _namespace.ReplayContexts.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetAll").ShouldBeTrue();
    [Fact] void should_not_change_jobs() => _namespace.Jobs.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetJobs").ShouldBeTrue();
}
