// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification;

public class when_the_join_has_an_explicit_event_source_id_key : given.a_classification_scenario
{
    void Establish() { JoinKey = new("$eventSourceId"); }

    void Because() => Classify();

    [Fact] void should_mark_the_join() => Result.Count.ShouldEqual(1);
}
