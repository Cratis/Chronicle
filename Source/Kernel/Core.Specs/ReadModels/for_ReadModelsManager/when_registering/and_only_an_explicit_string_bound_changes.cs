// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_only_an_explicit_string_bound_changes : given.a_read_models_manager
{
    ReadModelDefinition _first;
    ReadModelDefinition _second;
    static readonly ProjectionId _projection = "string-bound-projection";

    async Task Establish()
    {
        _first = DefinitionFor("bounded-model", "Bounded") with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string"}}}""") } };
        _second = _first with { Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","maxLength":80}}}""") } };
        _projectionsManagerGrain.GetProjectionDefinitions().Returns([ProjectionTargeting(_projection, _first.Identifier)]);
        await _manager.Register([_first]);
    }

    async Task Because() => await _manager.Register([_second]);

    [Fact] void should_store_the_bound_in_the_existing_generation() => _readModelGrain.Received(1).SetDefinition(Arg.Is<ReadModelDefinition>(definition => definition.Schemas.Count == 1 && definition.LatestGeneration == ReadModelGeneration.First && definition.Schemas[ReadModelGeneration.First].Properties["name"].MaxLength == 80));
    [Fact] void should_refresh_the_existing_pipeline_without_requesting_a_new_generation() => _projectionPipelines.Received(1).EvictFor(_eventStore, (EventStoreNamespaceName)"default", _projection);
}
