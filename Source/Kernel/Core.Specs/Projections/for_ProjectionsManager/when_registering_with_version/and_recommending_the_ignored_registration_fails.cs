// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_recommending_the_ignored_registration_fails : given.a_projections_manager_grain
{
    ProjectionDefinition _incoming;
    Exception _exception;

    void Establish()
    {
        var stored = CreateDefinition("the-projection", "the-read-model") with { Version = "2.0.0" };
        _incoming = CreateDefinition("the-projection", "the-read-model") with { Tags = ["Changed"] };
        _state.Projections = [stored];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
        _recommendationsManager
            .Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(Arg.Any<RecommendationDescription>(), Arg.Any<IgnoredProjectionRegistrationRequest>())
            .Returns<Task<RecommendationId>>(_ => throw new InvalidOperationException("The recommendations manager is unavailable"));
    }

    async Task Because() => _exception = await Catch.Exception(() => _grain.Register([_incoming], registrant: "the-app", version: "1.0.0"));

    [Fact] void should_not_fail_the_registration() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_stored_definition() => _state.Projections.Single().Version.ShouldEqual("2.0.0");
}
