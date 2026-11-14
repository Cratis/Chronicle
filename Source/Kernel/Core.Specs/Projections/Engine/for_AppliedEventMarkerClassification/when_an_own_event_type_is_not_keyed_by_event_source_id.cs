// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification;

public class when_an_own_event_type_is_not_keyed_by_event_source_id : given.a_classification_scenario
{
    void Establish() { OwnEventResolvesToEventSourceId = false; }

    void Because() => Classify();

    [Fact] void should_not_mark() => Result.ShouldBeEmpty();
}
