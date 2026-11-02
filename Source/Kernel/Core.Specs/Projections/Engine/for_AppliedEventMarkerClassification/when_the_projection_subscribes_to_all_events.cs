// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification;

public class when_the_projection_subscribes_to_all_events : given.a_classification_scenario
{
    void Establish() { SubscribesToAllEvents = true;}

    void Because() => Classify();

    [Fact] void should_not_mark() => Result.ShouldBeEmpty();
}
