// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeEligibility;

public class when_cursors_are_equal : Specification
{
    FailedPartition _first;
    FailedPartition _second;
    FailedPartition? _selected;

    void Establish()
    {
        _first = new() { Id = new Guid("00000000-0000-0000-0000-000000000001"), WebhookDisposition = WebhookFailureDisposition.Retryable, RecoveryCursor = new(10UL, FailedPartitionRecoveryMode.Live, null) };
        _second = new() { Id = new Guid("00000000-0000-0000-0000-000000000002"), WebhookDisposition = WebhookFailureDisposition.Configuration, RecoveryCursor = new(10UL, FailedPartitionRecoveryMode.Live, null) };
    }

    void Because() => _selected = WebhookProbeEligibility.Select([_second, _first], new WebhookProbeState(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, 0) { EpisodeId = null });

    [Fact] void should_select_the_expected_failure() => _selected.ShouldEqual(_first);
}
