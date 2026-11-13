// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints;

public class when_a_name_cannot_be_reindexed : given.a_reindex_command
{
    void Establish() => _definitions.Add(new UniqueConstraintDefinition("outbox-only", [new("Other", ["email"])]) { EventSequences = ["outbox"] });

    [Theory]
    [InlineData("unknown")]
    [InlineData("once")]
    [InlineData("outbox-only")]
    public async Task should_reject_instead_of_silently_ignoring_the_name(string name)
    {
        var result = await _scenario.Execute(new("store", "ns", "log", [name]));
        result.IsValid.ShouldBeFalse();
        await _jobs.DidNotReceiveWithAnyArgs().Start<IReindexConstraints, ReindexConstraintsRequest>(default!);
    }
}
