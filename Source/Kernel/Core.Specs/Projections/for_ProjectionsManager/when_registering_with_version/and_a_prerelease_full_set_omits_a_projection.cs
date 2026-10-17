// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_a_prerelease_full_set_omits_a_projection : given.a_projections_manager_grain
{
    [Theory]
    [InlineData("2.0.0-rc.2", "2.0.0-rc.10", true)]
    [InlineData("2.0.0-rc.10", "2.0.0-rc.2", false)]
    [InlineData("2.0.0-alpha", "2.0.0-alpha.1", true)]
    [InlineData("2.0.0-alpha.1", "2.0.0-alpha", false)]
    [InlineData("1.0.0-rc.1", "1.0.0", true)]
    [InlineData("1.0.0", "1.0.0-rc.1", false)]
    [InlineData("2.0.0-2", "2.0.0-alpha", true)]
    [InlineData("2.0.0-alpha", "2.0.0-2", false)]
    async Task should_retire_only_for_a_newer_full_set(string incomingVersion, string storedVersion, bool retained)
    {
        var shared = CreateDefinition("shared-projection", "shared-read-model") with { Version = incomingVersion };
        var omitted = CreateDefinition("omitted-projection", "omitted-read-model") with { Version = storedVersion };
        _state.Projections = [shared, omitted];
        _state.Registrants = new Dictionary<string, string> { ["shared-projection"] = "the-app", ["omitted-projection"] = "the-app" };
        _readModelDefinitions = [CreateReadModelDefinition("shared-read-model"), CreateReadModelDefinition("omitted-read-model", "omittedContainer")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);

        await _grain.Register([shared], ProjectionOwner.Client, "the-app", incomingVersion);

        _state.Projections.Contains(omitted).ShouldEqual(retained);
        await _observerGrain.Received(retained ? 0 : 1).Retire();
        await _projectionGrain.Received(retained ? 0 : 1).Remove();
    }
}
