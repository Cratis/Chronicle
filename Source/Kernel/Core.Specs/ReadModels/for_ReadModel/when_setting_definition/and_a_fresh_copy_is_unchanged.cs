// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModel.when_setting_definition;

public class and_a_fresh_copy_is_unchanged : Specification
{
    readonly TestKitSilo _silo = new();
    ReadModel _readModel;
    IStorage<ReadModelDefinition> _storage;

    static ReadModelDefinition FreshDefinition() => new(
        "model",
        "container",
        "Model",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "projection",
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema> { [(ReadModelGeneration)1] = JsonSchema.FromJson("{\"type\":\"object\"}") },
        [new IndexDefinition("name")]);

    async Task Establish()
    {
        _storage = Substitute.For<IStorage<ReadModelDefinition>>();
        _storage.State.Returns(FreshDefinition());
        _silo.Options.StorageFactory = _ => _storage;
        _readModel = await _silo.CreateGrainAsync<ReadModel>("model+store");
    }

    Task Because() => _readModel.SetDefinition(FreshDefinition());

    [Fact] async Task should_not_write_again() => await _storage.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_keep_the_registered_schema() => (await _readModel.GetDefinition()).Schemas.Count.ShouldEqual(1);
}
