// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering_full_set;

/// <summary>
/// A projection defined through the server (for example saved from the Workbench) is not part of a client's full
/// set and must survive it - retirement only applies to projections with the same owner as the registration.
/// </summary>
public class and_the_stored_projection_has_a_different_owner : given.a_projections_manager_grain
{
    ProjectionDefinition _clientProjection;
    ProjectionDefinition _serverProjection;

    void Establish()
    {
        _clientProjection = CreateDefinition("client-projection", "client-read-model");
        _serverProjection = CreateDefinition("server-projection", "server-read-model") with { Owner = ProjectionOwner.Server };
        _state.Projections = [_clientProjection, _serverProjection];
        _readModelDefinitions = [CreateReadModelDefinition("client-read-model"), CreateReadModelDefinition("server-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_clientProjection], ProjectionOwner.Client);

    [Fact] void should_keep_the_server_owned_projection() => _state.Projections.ShouldContainOnly(_clientProjection, _serverProjection);
    [Fact] void should_not_unsubscribe_any_observer() => _observerGrain.DidNotReceive().Unsubscribe();
    [Fact] void should_not_remove_any_projection_grain() => _projectionGrain.DidNotReceive().Remove();
}
