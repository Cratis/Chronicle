// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_AppliedEventMarkerClassification;

public class when_the_projection_is_global : given.a_classification_scenario
{
    void Establish() { Scope = Cratis.Chronicle.Concepts.Projections.ProjectionScope.Global;}

    void Because() => Classify();

    [Fact] void should_not_mark() => Result.ShouldBeEmpty();
}
