// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_an_older_registration_is_repeated_across_many_namespaces : given.a_projections_manager_grain
{
    const int NamespaceCount = 5;

    ProjectionDefinition _firstIncoming;
    ProjectionDefinition _secondIncoming;

    void Establish()
    {
        var first = CreateDefinition("first-projection", "the-read-model") with { Version = "2.0.0" };
        var second = CreateDefinition("second-projection", "the-read-model") with { Version = "2.0.0" };
        _firstIncoming = CreateDefinition("first-projection", "the-read-model") with { Tags = ["Changed"] };
        _secondIncoming = CreateDefinition("second-projection", "the-read-model") with { Tags = ["Changed"] };
        _state.Projections = [first, second];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _namespaces.GetAll().Returns(Enumerable.Range(0, NamespaceCount).Select(index => new EventStoreNamespaceName($"namespace-{index}")).ToArray());
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
    }

    async Task Because()
    {
        await _grain.Register([_firstIncoming, _secondIncoming], registrant: "the-app", version: "1.0.0");
        await _grain.Register([_firstIncoming, _secondIncoming], registrant: "the-app", version: "1.0.0");
    }

    [Fact] void should_recommend_each_projection_once_per_namespace() => _recommendationsManager.Received(2 * NamespaceCount).Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(
        Arg.Any<RecommendationDescription>(),
        Arg.Any<IgnoredProjectionRegistrationRequest>());
}
