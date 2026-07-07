// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RebuildConstraintIndexes;

public class when_applying_down : given.a_rebuild_constraint_indexes_patch
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(_patch.Down);

    [Fact] void should_not_throw() => _error.ShouldBeNull();

    [Fact] void should_not_start_any_jobs() =>
        _jobsManager.DidNotReceive().Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
}
