// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RederiveUniqueConstraintIndexes.when_applying_up;

public class and_no_rows_are_pending : given.a_rederive_patch
{
    async Task Because() => await _patch.Up();

    [Fact] void should_not_start_any_job() => _jobsManager.DidNotReceive().Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>());
}
