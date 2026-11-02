// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification;

public class when_the_join_is_keyed_by_an_event_property : given.a_classification_scenario
{
    void Establish() { JoinKey = new("customerId");}

    void Because() => Classify();

    [Fact] void should_not_mark() => Result.ShouldBeEmpty();
}
