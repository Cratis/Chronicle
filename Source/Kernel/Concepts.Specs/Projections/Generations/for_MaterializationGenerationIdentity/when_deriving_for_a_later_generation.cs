// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_MaterializationGenerationIdentity;

public class when_deriving_for_a_later_generation : Specification
{
    MaterializationGenerationIdentity _result;

    void Because() => _result = MaterializationGenerationIdentity.For("orders", 3U, "Order");

    [Fact] void should_give_the_generation_its_own_observer_identifier() => _result.ObserverId.ShouldEqual(new ObserverId("orders@g3"));
    [Fact] void should_give_the_generation_its_own_destination() => _result.Destination.ShouldEqual(new ReadModelContainerName("Order-g3"));
    [Fact] void should_keep_the_projection() => _result.ProjectionId.ShouldEqual(new ProjectionId("orders"));
    [Fact] void should_keep_the_generation() => _result.Generation.ShouldEqual(new MaterializationGeneration(3U));
}
