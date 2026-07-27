// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepCheckpointDebounce;

public class when_the_state_has_been_persisted : Specification
{
    JobStepCheckpointDebounce _debounce;

    void Establish()
    {
        _debounce = new JobStepCheckpointDebounce(3);
        _debounce.Report(afterEveryBatch: false);
    }

    void Because() => _debounce.Persisted();

    [Fact] void should_have_no_pending_checkpoint() => _debounce.HasPendingCheckpoint.ShouldBeFalse();
}
