// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue;

public class when_checking_batch_event_types : given.subscription_matching
{
    bool _overlap;
    bool _disjoint;
    bool _redaction;
    bool _wildcard;

    void Because()
    {
        _overlap = MayMatch(_subscription, ["matching", "other"], false);
        _disjoint = MayMatch(_subscription, ["other"], false);
        _redaction = MayMatch(_subscription, ["other"], true);
        _wildcard = MayMatch(_subscription with { AllEventTypes = true }, [], false);
    }

    [Fact] void should_accept_overlapping_types() => _overlap.ShouldBeTrue();
    [Fact] void should_reject_disjoint_types() => _disjoint.ShouldBeFalse();
    [Fact] void should_allow_redactions_through_the_cheap_check() => _redaction.ShouldBeTrue();
    [Fact] void should_allow_a_wildcard_subscription() => _wildcard.ShouldBeTrue();
}
