// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_the_incoming_version_is_newer : given.a_projections_manager_grain
{
    ProjectionDefinition _incoming;

    void Establish()
    {
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { Version = "1.0.0" }];
        _incoming = CreateDefinition("the-projection", "the-read-model") with { Tags = ["Changed"] };
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
    }

    async Task Because() => await _grain.Register([_incoming], version: "2.0.0");

    [Fact] void should_replace_the_stored_definition() => _state.Projections.Single().Tags!.ShouldContainOnly("Changed");
    [Fact] void should_store_the_new_version() => _state.Projections.Single().Version.ShouldEqual("2.0.0");
    [Fact] async Task should_set_the_projection_definition() => await _projectionGrain.Received(1).SetDefinition(Arg.Is<ProjectionDefinition>(definition => definition.Version == "2.0.0"));
    [Fact] void should_not_recommend_anything() => _recommendationsManager.DidNotReceiveWithAnyArgs().Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(default!, default!);
}
