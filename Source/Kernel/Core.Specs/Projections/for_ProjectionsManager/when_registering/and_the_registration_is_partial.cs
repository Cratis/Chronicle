// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

/// <summary>
/// A partial registration - saving a single projection, for example - must never retire anything: only a
/// registration that declares itself the full set for an owner may retire what that owner no longer registers.
/// </summary>
public class and_the_registration_is_partial : given.a_projections_manager_grain
{
    ProjectionDefinition _registered;
    ProjectionDefinition _other;

    void Establish()
    {
        _registered = CreateDefinition("registered-projection", "registered-read-model");
        _other = CreateDefinition("other-projection", "other-read-model");
        _state.Projections = [_registered, _other];
        _readModelDefinitions = [CreateReadModelDefinition("registered-read-model"), CreateReadModelDefinition("other-read-model")];

        _definitionComparer
            .Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because() => await _grain.Register([_registered]);

    [Fact] void should_keep_every_registered_projection() => _state.Projections.ShouldContainOnly(_registered, _other);
    [Fact] void should_not_unsubscribe_any_observer() => _observerGrain.DidNotReceive().Unsubscribe();
    [Fact] void should_not_remove_any_projection_grain() => _projectionGrain.DidNotReceive().Remove();
}
