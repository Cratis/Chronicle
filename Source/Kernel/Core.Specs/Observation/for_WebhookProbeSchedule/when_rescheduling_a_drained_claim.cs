// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeSchedule;

public class when_rescheduling_a_drained_claim : Specification
{
    WebhookProbeState _control;
    WebhookProbeState _next;

    void Establish() => _control = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UnixEpoch, int.MaxValue)
    {
        DrainPending = false, EpisodeId = FailedPartitionId.New(), Position = 42UL, Partition = "partition", DefinitionFingerprint = "definition"
    };
    void Because() => _next = WebhookProbeSchedule.Reschedule(_control, DateTimeOffset.UnixEpoch.AddSeconds(60), DateTimeOffset.UnixEpoch.AddSeconds(120));

    [Fact] void should_honor_the_later_receiver_deadline() => _next.Due.ShouldEqual(DateTimeOffset.UnixEpoch.AddSeconds(120));
    [Fact] void should_saturate_the_failure_count() => _next.Failures.ShouldEqual(int.MaxValue);
    [Fact] void should_preserve_the_generation() => _next.Id.ShouldEqual(_control.Id);
    [Fact] void should_clear_the_episode() => _next.EpisodeId.ShouldBeNull();
    [Fact] void should_clear_the_position() => _next.Position.ShouldBeNull();
    [Fact] void should_clear_the_partition() => _next.Partition.ShouldBeNull();
    [Fact] void should_clear_the_definition() => _next.DefinitionFingerprint.ShouldBeNull();
}
