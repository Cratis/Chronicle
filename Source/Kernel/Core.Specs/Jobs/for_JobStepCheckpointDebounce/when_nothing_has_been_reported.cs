// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepCheckpointDebounce;

public class when_nothing_has_been_reported : Specification
{
    JobStepCheckpointDebounce _debounce;

    void Because() => _debounce = new JobStepCheckpointDebounce(3);

    [Fact] void should_have_no_pending_checkpoint() => _debounce.HasPendingCheckpoint.ShouldBeFalse();
    [Fact] void should_never_go_below_one_batch() => new JobStepCheckpointDebounce(0).BatchInterval.ShouldEqual(1);
}
