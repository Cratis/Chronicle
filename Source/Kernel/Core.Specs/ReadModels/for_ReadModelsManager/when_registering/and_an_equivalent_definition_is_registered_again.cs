// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

/// <summary>
/// A reconnecting client sends freshly deserialized definitions whose schema dictionary and indexes are different
/// instances; re-registering these must not push anything to the read model grain.
/// </summary>
public class and_an_equivalent_definition_is_registered_again : given.a_read_models_manager
{
    async Task Establish()
    {
        await _manager.Register([Create()]);
        _readModelGrain.ClearReceivedCalls();
    }

    async Task Because() => await _manager.Register([Create()]);

    [Fact] void should_not_set_the_definition_on_the_read_model() => _readModelGrain.DidNotReceiveWithAnyArgs().SetDefinition(default!);

    static ReadModelDefinition Create() => new(
        "some-read-model",
        "some-container",
        "Some read model",
        ReadModelOwner.None,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "some-projection",
        new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
        new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, JsonSchema.FromJson("{\"type\":\"object\"}") } },
        [new IndexDefinition(new PropertyPath("Name"))]);
}
