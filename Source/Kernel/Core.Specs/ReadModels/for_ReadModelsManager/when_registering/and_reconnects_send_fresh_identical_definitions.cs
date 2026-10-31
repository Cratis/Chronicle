// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_reconnects_send_fresh_identical_definitions : given.a_read_models_manager
{
    static ReadModelDefinition FreshDefinition() => DefinitionFor("model", "Model") with
    {
        Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [(ReadModelGeneration)1] = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}")
        },
        Indexes = [new IndexDefinition("name")]
    };

    async Task Establish()
    {
        await _manager.Register([FreshDefinition()]);
        _readModelGrain.ClearReceivedCalls();
        _silo.StorageStats<ReadModelsManager, ReadModelsManagerState>().ResetCounts();
    }

    async Task Because()
    {
        // Orleans serializes turns on this non-reentrant manager; model the queued reconnects as turns.
        for (var reconnect = 0; reconnect < 40; reconnect++)
        {
            await _manager.Register([FreshDefinition()]);
        }
    }

    [Fact] async Task should_not_write_any_read_model_definition() => await _readModelGrain.DidNotReceive().SetDefinition(Arg.Any<ReadModelDefinition>());
    [Fact] void should_not_rewrite_the_managers_entire_definition_set() => _silo.StorageStats<ReadModelsManager, ReadModelsManagerState>().Writes.ShouldEqual(0);
    [Fact] async Task should_keep_one_registered_definition() => (await _manager.GetDefinitions()).Count().ShouldEqual(1);
}
