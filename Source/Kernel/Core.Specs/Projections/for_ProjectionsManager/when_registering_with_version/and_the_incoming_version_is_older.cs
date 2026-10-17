// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_the_incoming_version_is_older : given.a_projections_manager_grain
{
    ProjectionDefinition _stored;
    ProjectionDefinition _incoming;

    void Establish()
    {
        _stored = CreateDefinition("the-projection", "the-read-model") with { Version = "2.0.0" };
        _incoming = CreateDefinition("the-projection", "the-read-model") with { Tags = ["Changed"] };
        _state.Projections = [_stored];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
    }

    async Task Because() => await _grain.Register([_incoming], registrant: "the-app", version: "1.0.0");

    [Fact] void should_keep_the_stored_definition() => _state.Projections.Single().ShouldEqual(_stored);
    [Fact] void should_not_register_with_the_engine() => _projectionsServiceClient.DidNotReceiveWithAnyArgs().Register(default!, default!);
    [Fact] void should_not_set_the_projection_definition() => _projectionGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);
    [Fact] void should_not_record_the_registrant() => _state.Registrants.ShouldBeEmpty();
    [Fact] void should_recommend_looking_at_the_ignored_registration() => _recommendationsManager.Received(1).Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(
        Arg.Is<RecommendationDescription>(description => description.Value.Contains("1.0.0") && description.Value.Contains("2.0.0")),
        Arg.Is<IgnoredProjectionRegistrationRequest>(request => request.ProjectionId == "the-projection" && request.IncomingVersion == "1.0.0" && request.StoredVersion == "2.0.0"));
}
