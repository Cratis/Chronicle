// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs.for_JobStepCheckpointDebounce;

/// <summary>
/// A collapsing projection cannot make a redelivered batch a no-op, so its step overrides the configured interval
/// and persists after every batch.
/// </summary>
public class when_the_step_must_checkpoint_after_every_batch : Specification
{
    JobStepCheckpointDebounce _debounce;
    bool _shouldWrite;

    void Establish() => _debounce = new JobStepCheckpointDebounce(100);

    void Because() => _shouldWrite = _debounce.Report(afterEveryBatch: true);

    [Fact] void should_ask_for_a_write() => _shouldWrite.ShouldBeTrue();
}
