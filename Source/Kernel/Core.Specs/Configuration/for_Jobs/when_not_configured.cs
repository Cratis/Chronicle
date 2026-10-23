// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Jobs;

public class when_not_configured : Specification
{
    Jobs _jobs;

    void Because() => _jobs = new();

    [Fact] void should_checkpoint_replay_every_ten_thousand_events() => _jobs.ReplayCheckpointEventInterval.ShouldEqual(10_000);
    [Fact] void should_debounce_checkpoints_over_one_hundred_batches() => _jobs.StepCheckpointBatchInterval.ShouldEqual(100);
    [Fact] void should_flush_pending_checkpoints_after_five_seconds() => _jobs.StepCheckpointFlushInterval.ShouldEqual(TimeSpan.FromSeconds(5));
}
