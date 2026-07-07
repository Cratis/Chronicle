// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RebuildConstraintIndexes.when_applying_up;

public class and_no_unique_constraints_are_registered : given.a_rebuild_constraint_indexes_patch
{
    void Establish() =>
        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>(
            [new UniqueEventTypeConstraintDefinition("SomeConstraint", "SomeEvent")]));

    async Task Because() => await _patch.Up();

    [Fact] void should_not_start_reindex_job() =>
        _jobsManager.DidNotReceive().Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
}
