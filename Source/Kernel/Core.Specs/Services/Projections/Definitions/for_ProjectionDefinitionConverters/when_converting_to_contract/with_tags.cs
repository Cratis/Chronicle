// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Services.Projections.Definitions.for_ProjectionDefinitionConverters.when_converting_to_contract;

public class with_tags : Specification
{
    ProjectionDefinition _definition;
    Contracts.Projections.ProjectionDefinition _contract;

    void Establish() => _definition = Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain.CreateDefinition("the-projection", "the-read-model") with
    {
        Tags = ["Orders", "Analytics"]
    };

    void Because() => _contract = _definition.ToContract();

    [Fact] void should_preserve_the_tags() => _contract.Tags.ShouldContainOnly("Orders", "Analytics");
    [Fact] void should_round_trip_the_tags() => _contract.ToChronicle(ProjectionOwner.Client).Tags!.ShouldContainOnly("Orders", "Analytics");
}
