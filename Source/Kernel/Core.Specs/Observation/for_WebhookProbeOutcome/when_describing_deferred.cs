// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_WebhookProbeOutcome;

public class when_describing_deferred : Specification
{
    string _reason;

    void Because() => _reason = WebhookProbeOutcome.FailureReason(new(ObserverSubscriberState.Deferred, EventSequenceNumber.Unavailable, [], string.Empty));

    [Fact] void should_use_the_existing_reason() => _reason.ShouldEqual("deferred");
}
