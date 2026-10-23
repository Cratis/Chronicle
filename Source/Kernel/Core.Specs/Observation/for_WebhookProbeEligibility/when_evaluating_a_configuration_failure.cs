// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeEligibility;

public class when_evaluating_a_configuration_failure : Specification
{
    bool _eligible;

    void Because() => _eligible = WebhookProbeEligibility.IsEligible(new FailedPartition { WebhookDisposition = WebhookFailureDisposition.Configuration });

    [Fact] void should_allow_probing() => _eligible.ShouldBeTrue();
}
