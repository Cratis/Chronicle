// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

/// <summary>
/// The common case - a same-version client reconnecting - re-sends an identical definition on every reconnect;
/// re-evicting every projection targeting it on every reconnect would make that no longer free.
/// </summary>
public class and_an_existing_definition_is_unchanged : given.a_read_models_manager
{
    static readonly ReadModelDefinition _definition = DefinitionFor("some-read-model", "Display name");
    static readonly ProjectionId _targetingProjection = "some-projection";

    async Task Establish()
    {
        _projectionsManagerGrain.GetProjectionDefinitions().Returns([ProjectionTargeting(_targetingProjection, _definition.Identifier)]);
        await _manager.Register([_definition]);
        _projectionPipelines.ClearReceivedCalls();
    }

    async Task Because() => await _manager.Register([_definition with { }]);

    [Fact] void should_not_evict_any_pipeline() => _projectionPipelines.DidNotReceive().EvictFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionId>());
}
