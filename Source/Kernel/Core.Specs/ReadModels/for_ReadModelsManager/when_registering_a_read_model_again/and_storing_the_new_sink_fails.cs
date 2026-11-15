// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering_a_read_model_again;

/// <summary>
/// The kernel stops after recording the replays but before storing the new sink: the next registration must find the change again.
/// </summary>
public class and_storing_the_new_sink_fails : Specification
{
    readonly TestKitSilo _silo = new();
    IStorage<ReadModelsManagerState> _storage;
    IReadModel _readModel;
    ISinkChangeReplays _replays;
    ReadModelsManager _manager;
    Exception _error;
    IEnumerable<ReadModelDefinition> _storedAfterFailure;

    async Task Establish()
    {
        _storage = Substitute.For<IStorage<ReadModelsManagerState>>();
        _storage.State = new ReadModelsManagerState();
        _silo.Options.StorageFactory = _ => _storage;
        _readModel = Substitute.For<IReadModel>();
        _silo.AddProbe(_ => _readModel);
        _silo.AddProbe(_ => Substitute.For<IProjectionsManager>());
        _silo.AddService(Substitute.For<IProjectionPipelineManager>());
        _silo.AddService<ISinkConfigurations>(new SinkConfigurations(Options.Create(new ChronicleOptions
        {
            Sinks = new()
            {
                Configurations = new Dictionary<string, SinkConfiguration>(StringComparer.OrdinalIgnoreCase)
                {
                    ["reporting"] = new() { Type = "SQL", ConnectionDetails = "Host=pg;Database=reporting" }
                }
            }
        })));
        _replays = Substitute.For<ISinkChangeReplays>();
        _replays.Request(Arg.Any<EventStoreName>(), Arg.Any<ReadModelIdentifier>()).Returns(call => new SinkChangeReplay(call.ArgAt<EventStoreName>(0), call.ArgAt<ReadModelIdentifier>(1), []));
        _silo.AddService(_replays);
        _manager = await _silo.CreateGrainAsync<ReadModelsManager>("store");
        await _manager.Register([Orders(SinkName.Default)]);

        _storage.WriteStateAsync().Returns(Task.FromException(new InvalidOperationException("The kernel stopped")));
        _error = await Catch.Exception(() => _manager.Register([Orders("reporting")]));
        _storedAfterFailure = await _manager.GetDefinitions();
        _storage.WriteStateAsync().Returns(Task.CompletedTask);
    }

    Task Because() => _manager.Register([Orders("reporting")]);

    [Fact] void should_fail_the_registration() => _error.ShouldNotBeNull();
    [Fact] void should_leave_the_stored_sink_unchanged() => _storedAfterFailure.Single().Sink.Name.ShouldEqual(SinkName.Default);
    [Fact] void should_request_the_replays_again() => _replays.Received(2).Request("store", (ReadModelIdentifier)"orders");
    [Fact] void should_start_the_replays_only_once_the_new_sink_is_stored() => _replays.ReceivedWithAnyArgs(1).Start(default!);

    static ReadModelDefinition Orders(SinkName name) =>
        given.a_read_models_manager.DefinitionFor("orders", "Orders") with { Sink = new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB) { Name = name } };
}
