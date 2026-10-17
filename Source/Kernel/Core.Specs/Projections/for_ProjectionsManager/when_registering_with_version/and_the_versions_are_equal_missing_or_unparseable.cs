// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_the_versions_are_equal_missing_or_unparseable : given.a_projections_manager_grain
{
    void Establish()
    {
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Different);
    }

    [Theory]
    [InlineData("2.0.0", "2.0.0")]
    [InlineData("2.0.0", null)]
    [InlineData(null, "1.0.0")]
    [InlineData("2.0.0", "garbage")]
    [InlineData("garbage", "1.0.0")]
    async Task should_register_as_it_did_before_versions(string? storedVersion, string? incomingVersion)
    {
        _state.Projections = [CreateDefinition("the-projection", "the-read-model") with { Version = storedVersion }];
        var incoming = CreateDefinition("the-projection", "the-read-model") with { Tags = ["Changed"] };

        await _grain.Register([incoming], version: incomingVersion);

        _state.Projections.Single().Tags!.ShouldContainOnly("Changed");
        await _projectionGrain.Received(1).SetDefinition(Arg.Any<ProjectionDefinition>());
        _ = _recommendationsManager.DidNotReceiveWithAnyArgs().Add<IIgnoredProjectionRegistrationRecommendation, IgnoredProjectionRegistrationRequest>(default!, default!);
    }
}
