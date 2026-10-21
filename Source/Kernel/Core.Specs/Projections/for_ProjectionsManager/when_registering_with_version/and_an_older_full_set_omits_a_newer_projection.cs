// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_with_version;

public class and_an_older_full_set_omits_a_newer_projection : given.a_projections_manager_grain
{
    ProjectionDefinition _added;
    ProjectionDefinition _shared;

    void Establish()
    {
        _shared = CreateDefinition("shared-projection", "shared-read-model") with { Version = "1.0.0" };
        _added = CreateDefinition("added-projection", "added-read-model") with { Version = "2.0.0" };
        _state.Projections = [_shared, _added];
        _state.Registrants = new Dictionary<string, string> { ["shared-projection"] = "the-app", ["added-projection"] = "the-app" };
        _readModelDefinitions = [CreateReadModelDefinition("shared-read-model"), CreateReadModelDefinition("added-read-model")];
        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([CreateDefinition("shared-projection", "shared-read-model")], ProjectionOwner.Client, "the-app", "1.0.0");

    [Fact] void should_keep_the_newer_projection() => _state.Projections.Select(_ => _.Identifier.Value).ShouldContain("added-projection");
    [Fact] void should_not_unregister_it_from_the_engine() => _projectionsServiceClient.DidNotReceive().Unregister(Arg.Any<EventStoreName>(), Arg.Any<ProjectionId>());
    [Fact] async Task should_not_retire_its_observer() => await _observerGrain.DidNotReceive().Retire();
}
