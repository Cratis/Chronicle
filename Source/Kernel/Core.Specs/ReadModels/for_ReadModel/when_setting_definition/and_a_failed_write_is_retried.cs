// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModel.when_setting_definition;

public class and_a_failed_write_is_retried : Specification
{
    readonly TestKitSilo _silo = new();
    ReadModel _readModel;
    IStorage<ReadModelDefinition> _storage;
    ReadModelDefinition _previous;
    ReadModelDefinition _incoming;
    ReadModelDefinition _afterFailure;
    Exception? _failure;

    async Task Establish()
    {
        _previous = new(
            "model",
            "container",
            "Old name",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "projection",
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []);
        _incoming = _previous with { DisplayName = "New name" };
        _storage = Substitute.For<IStorage<ReadModelDefinition>>();
        _storage.State = _previous;
        _storage.WriteStateAsync().Returns(Task.FromException(new ReadModelNotFound(_incoming.Identifier)), Task.CompletedTask);
        _silo.Options.StorageFactory = _ => _storage;
        _readModel = await _silo.CreateGrainAsync<ReadModel>("model+store");
    }

    async Task Because()
    {
        _failure = await Catch.Exception(() => _readModel.SetDefinition(_incoming));
        _afterFailure = await _readModel.GetDefinition();
        await _readModel.SetDefinition(_incoming);
    }

    [Fact] void should_surface_the_failed_write() => _failure.ShouldBeOfExactType<ReadModelNotFound>();
    [Fact] void should_restore_the_previous_accepted_definition() => _afterFailure.ShouldEqual(_previous);
    [Fact] async Task should_retry_the_write() => await _storage.Received(2).WriteStateAsync();
    [Fact] async Task should_accept_the_successful_write() => (await _readModel.GetDefinition()).ShouldEqual(_incoming);
}
