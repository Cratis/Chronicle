// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepCheckpointDebounce;

public class when_reporting_fewer_batches_than_the_interval : Specification
{
    JobStepCheckpointDebounce _debounce;
    bool[] _shouldWrite;

    void Establish() => _debounce = new JobStepCheckpointDebounce(3);

    void Because() => _shouldWrite = [.. Enumerable.Range(0, 2).Select(_ => _debounce.Report(afterEveryBatch: false))];

    [Fact] void should_not_ask_for_a_write() => _shouldWrite.ShouldEachConformTo(_ => !_);
    [Fact] void should_leave_a_pending_checkpoint() => _debounce.HasPendingCheckpoint.ShouldBeTrue();
}
