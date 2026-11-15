// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_no_constraints_apply : given.a_reindex_command
{
    void Establish() => _definitions.Clear();

    async Task Because() => _result = await _scenario.Execute(new("store", "ns", "log", []));

    [Fact] void should_reject_the_command() => _result.IsValid.ShouldBeFalse();
    [Fact] async Task should_not_start_a_job() => await _jobs.DidNotReceiveWithAnyArgs().Start<IReindexConstraints, ReindexConstraintsRequest>(default!);
}
