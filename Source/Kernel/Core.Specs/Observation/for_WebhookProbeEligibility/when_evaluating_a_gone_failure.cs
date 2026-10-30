// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeEligibility;

public class when_evaluating_a_gone_failure : Specification
{
    bool _eligible;

    void Because() => _eligible = WebhookProbeEligibility.IsEligible(new FailedPartition { WebhookDisposition = WebhookFailureDisposition.Gone });

    [Fact] void should_exclude_the_failure() => _eligible.ShouldBeFalse();
}
