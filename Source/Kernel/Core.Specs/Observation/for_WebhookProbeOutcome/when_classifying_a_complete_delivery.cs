// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_WebhookProbeOutcome;

public class when_classifying_a_complete_delivery : Specification
{
    bool _successful;

    void Because() => _successful = WebhookProbeOutcome.IsSuccessful(ObserverSubscriberResult.Ok(42UL), 42UL);

    [Fact] void should_confirm_delivery() => _successful.ShouldBeTrue();
}
