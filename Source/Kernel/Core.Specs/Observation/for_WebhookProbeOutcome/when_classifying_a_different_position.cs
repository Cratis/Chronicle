// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_WebhookProbeOutcome;

public class when_classifying_a_different_position : Specification
{
    bool _successful;

    void Because() => _successful = WebhookProbeOutcome.IsSuccessful(ObserverSubscriberResult.Ok(41UL), 42UL);

    [Fact] void should_not_confirm_delivery() => _successful.ShouldBeFalse();
}
