// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Jobs;
using Cratis.Monads;

using Catch = Cratis.Specifications.Catch;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RebuildConstraintIndexes.when_applying_up;

public class and_starting_the_job_fails : given.a_rebuild_constraint_indexes_patch
{
    Exception? _error;

    void Establish()
    {
        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>(
            [new UniqueConstraintDefinition("SomeUniqueConstraint", [new("SomeEvent", ["SomeProperty"])])]));

        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));
    }

    async Task Because() => _error = await Catch.Exception(_patch.Up);

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
