// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_EventFeatureExtractor.when_extracting;

/// <summary>
/// "Default" is the absence of an event source type, not a type worth mining - carrying it as a facet would put
/// every store that never named its event source types under one meaningless value.
/// </summary>
public class from_an_event_on_a_default_event_source_type : given.an_extractor
{
    EventFeatures _default;
    EventFeatures _unspecified;

    void Because()
    {
        _default = _extractor.Extract(AnEvent(eventSourceType: EventSourceType.Default));
        _unspecified = _extractor.Extract(AnEvent(eventSourceType: EventSourceType.Unspecified));
    }

    [Fact] void should_not_carry_an_aggregate_type_for_the_default() => _default.AggregateType.ShouldEqual(FacetValue.Unspecified);
    [Fact] void should_not_carry_an_aggregate_type_for_the_unspecified() => _unspecified.AggregateType.ShouldEqual(FacetValue.Unspecified);
}
