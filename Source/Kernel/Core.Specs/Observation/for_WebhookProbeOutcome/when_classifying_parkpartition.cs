// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_WebhookProbeOutcome;

public class when_classifying_parkpartition : Specification
{
    WebhookFailureDisposition _disposition;

    void Because() => _disposition = WebhookProbeOutcome.Disposition(WebhookDeliveryFailurePolicy.ParkPartition);

    [Fact] void should_preserve_the_receiver_policy() => _disposition.ShouldEqual(WebhookFailureDisposition.Gone);
}
