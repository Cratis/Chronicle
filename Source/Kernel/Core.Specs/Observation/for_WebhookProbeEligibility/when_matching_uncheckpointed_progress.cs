// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeEligibility;

public class when_matching_uncheckpointed_progress : Specification
{
    bool _matches;

    void Because() => _matches = WebhookProbeEligibility.MatchesPosition(
        new WebhookProbeState(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, 0) { Position = 9UL, ResumePosition = 10UL, ResumePending = false },
        new FailedPartition { RecoveryCursor = new(10UL, FailedPartitionRecoveryMode.Live, null) });

    [Fact] void should_reject_the_position() => _matches.ShouldBeFalse();
}
