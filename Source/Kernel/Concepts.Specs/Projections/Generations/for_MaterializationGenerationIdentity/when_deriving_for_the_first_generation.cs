// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_MaterializationGenerationIdentity;

public class when_deriving_for_the_first_generation : Specification
{
    MaterializationGenerationIdentity _result;

    void Because() => _result = MaterializationGenerationIdentity.For("orders", MaterializationGeneration.First, "Order");

    [Fact] void should_adopt_the_projection_identifier_as_observer_identifier() => _result.ObserverId.ShouldEqual(new ObserverId("orders"));
    [Fact] void should_adopt_the_existing_container() => _result.Destination.ShouldEqual(new ReadModelContainerName("Order"));
    [Fact] void should_keep_the_projection() => _result.ProjectionId.ShouldEqual(new ProjectionId("orders"));
}
