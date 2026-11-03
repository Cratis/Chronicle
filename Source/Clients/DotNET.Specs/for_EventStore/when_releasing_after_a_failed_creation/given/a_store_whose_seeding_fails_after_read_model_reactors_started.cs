// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.for_EventStore.when_releasing_after_a_failed_creation.given;

/// <summary>
/// An event store that starts its read-model reactors, and through them a read-model watcher, and then fails on
/// seeding. This is the order RegisterAll runs in, and it is the failure that leaves a watcher subscribed to the
/// shared connection with a server stream open.
/// </summary>
public class a_store_whose_seeding_fails_after_read_model_reactors_started : when_registering_all.given.an_event_store_with_a_projection_that_cannot_be_built
{
    protected static readonly Exception _failure = new("the kernel refused the seeding call");

    protected RecordingConnectionLifecycle _lifecycle;
    protected Subject<Contracts.ReadModels.ReadModelChangeset> _serverStream;
    protected ReadModelWatcherManager _watchers;
    protected Contracts.ReadModels.IReadModels _readModelsService;
    protected int _baselineConnected;
    protected int _baselineDisconnected;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(BuildableProjection)]);
        _clientArtifacts.ReadModelReactors.Returns([typeof(BuildableModelReactor)]);

        _lifecycle = new RecordingConnectionLifecycle();
        _serverStream = new Subject<Contracts.ReadModels.ReadModelChangeset>();
        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _readModelsService.Watch(Arg.Any<Contracts.ReadModels.WatchRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(_serverStream);

        var services = Substitute.For<IServices>();
        services.ReadModels.Returns(_readModelsService);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(_lifecycle);
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        SetAutoProperty("Connection", connection);

        _watchers = new ReadModelWatcherManager(new ReadModelWatcherFactory(_eventStore, JsonSerializerOptions.Default));
        SetField("_readModelWatchers", _watchers);

        var readModels = Substitute.For<IReadModels>();
        readModels.Watch<BuildableModel>().Returns(_ => _watchers.GetWatcher<BuildableModel>().Observable);
        SetAutoProperty("ReadModels", readModels);
        SetAutoProperty("ReadModelReactors", new ReadModelReactors(
            _eventStore,
            _clientArtifacts,
            Substitute.For<IClientArtifactsActivator>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IReadModelReactorInvoker>(),
            JsonSerializerOptions.Default,
            NullLogger<ReadModelReactors>.Instance));

        _eventStore.Seeding.Register().Returns(Task.FromException(_failure));

        // The failed run starts the background retry. Park it between attempts so it does not re-enter RegisterAll.
        SetField("_registrationBackoff", new Registrations.RegistrationBackoff(TimeSpan.FromHours(1), TimeSpan.FromHours(1)));

        _baselineConnected = _lifecycle.ConnectedCount;
        _baselineDisconnected = _lifecycle.DisconnectedCount;
    }

    public class BuildableModelReactor
    {
        public Task Added(BuildableModel model) => Task.CompletedTask;
    }
}
