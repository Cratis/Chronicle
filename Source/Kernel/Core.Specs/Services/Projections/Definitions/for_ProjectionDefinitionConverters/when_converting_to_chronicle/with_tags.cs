// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Services.Projections.Definitions.for_ProjectionDefinitionConverters.when_converting_to_chronicle;

public class with_tags : Specification
{
    Contracts.Projections.ProjectionDefinition _contract;
    ProjectionDefinition _definition;

    void Establish() => _contract = new()
    {
        EventSequenceId = "event-log",
        Identifier = "the-projection",
        ReadModel = "the-read-model",
        InitialModelState = "{}",
        Tags = ["Orders", "Analytics"]
    };

    void Because() => _definition = _contract.ToChronicle(ProjectionOwner.Client);

    [Fact] void should_preserve_the_tags() => _definition.Tags!.ShouldContainOnly("Orders", "Analytics");
    [Fact] void should_round_trip_the_tags() => _definition.ToContract().Tags.ShouldContainOnly("Orders", "Analytics");
}
