// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeEligibility;

public class when_rescheduling_a_success_checkpoint : Specification
{
    WebhookProbeState _control;
    bool _allowed;

    void Establish() => _control = new(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, 0) { ResumePending = true };
    void Because() => _allowed = WebhookProbeEligibility.CanReschedule(_control, _control);

    [Fact] void should_reject_rescheduling() => _allowed.ShouldBeFalse();
}
