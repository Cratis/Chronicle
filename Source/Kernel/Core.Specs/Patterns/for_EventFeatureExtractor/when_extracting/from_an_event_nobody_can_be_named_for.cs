// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

public class from_an_event_nobody_can_be_named_for : given.an_extractor
{
    EventFeatures _result;

    void Because() => _result = _extractor.Extract(AnEvent(causedBy: Identity.NotSet));

    [Fact] void should_not_produce_a_scope() => _result.GroupingKey.IsSpecified.ShouldBeFalse();
    [Fact] void should_not_claim_to_know_the_initiator_type() => _result.InitiatorType.ShouldEqual(InitiatorType.Unknown);
    [Fact] void should_not_carry_an_initiator() => _result.InitiatorId.ShouldEqual(FacetValue.Unspecified);
}
