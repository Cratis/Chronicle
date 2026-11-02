// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification;

public class when_marking_a_join : given.a_classification_scenario
{
    void Because() => Classify();

    [Fact] void should_use_the_definition_id_of_the_event_type_and_path() => Result[0].DefId.ShouldEqual(AppliedEventMarkers.DefinitionId(JoinEvent.Id.Value, "customerId"));
    [Fact] void should_carry_the_event_type_id() => Result[0].EventTypeId.ShouldEqual(JoinEvent.Id.Value);
    [Fact] void should_carry_the_path() => Result[0].OnPath.ShouldEqual("customerId");
}
