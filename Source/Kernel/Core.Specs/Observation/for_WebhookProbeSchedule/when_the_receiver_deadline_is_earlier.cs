// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeSchedule;

public class when_the_receiver_deadline_is_earlier : Specification
{
    WebhookProbeState _next;

    void Because() => _next = WebhookProbeSchedule.Reschedule(new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UnixEpoch, 0), DateTimeOffset.UnixEpoch.AddSeconds(120), DateTimeOffset.UnixEpoch.AddSeconds(60));

    [Fact] void should_keep_the_sampled_deadline() => _next.Due.ShouldEqual(DateTimeOffset.UnixEpoch.AddSeconds(120));
    [Fact] void should_increment_the_failure_count() => _next.Failures.ShouldEqual(1);
}
