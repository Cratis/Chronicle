// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification;

public class when_the_projection_is_keyed_by_event_source_id_with_a_root_join : given.a_classification_scenario
{
    void Establish() {}

    void Because() => Classify();

    [Fact] void should_mark_the_join() => Result.Count.ShouldEqual(1);
}
