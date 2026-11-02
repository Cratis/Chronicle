// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Serialization;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.LegacyIdentifierUpgrade;

public sealed class LegacyIdentifierClient : IDisposable
{
    public const string Store = "legacy-identifier-upgrade-5512";
    public const string Model = "legacy-identifier-5512";
    public const string Table = "LegacyIdentifier5512";
    public const string Projection = "legacy-identifier-projection-5512";
    public const string Schema = """
        {"$schema":"http://json-schema.org/draft-07/schema#","type":"object","properties":{"ID":{"type":"string"},"Name":{"type":"string"}}}
        """;

    readonly ChronicleClient _client;
    IEventStore _store = null!;

    public LegacyIdentifierClient(ChronicleConnectionString connection)
    {
        var options = new ChronicleOptions(connection) { ConnectTimeout = 30 };
        options.ExplicitArtifacts.RegisterEventType<LegacyIdentifierNamed>();
        _client = new(options, namingPolicy: new CamelCaseNamingPolicy());
    }

    IServices Services => ((IChronicleServicesAccessor)_store.Connection).Services;

    public async Task Register(CancellationToken token)
    {
        _store = await _client.GetEventStore(Store).WaitAsync(token);
        await _store.Connection.Connect().WaitAsync(token);
        _store.Connection.Lifecycle.IsConnected.ShouldBeTrue();
        await Services.ReadModels.RegisterSingle(new()
        {
            EventStore = Store,
            Owner = ReadModelOwner.Client,
            ReadModel = new()
            {
                Type = new() { Identifier = Model, Generation = 1 },
                ContainerName = Table,
                DisplayName = Model,
                Schema = Schema,
                Sink = new() { TypeId = "SQL", ConfigurationId = Guid.Empty },
                ObserverType = ReadModelObserverType.Projection,
                ObserverIdentifier = Projection,
                Owner = ReadModelOwner.Client
            }
        }).WaitAsync(token);
        var definitions = await Services.ReadModels.GetDefinitions(new() { EventStore = Store }).WaitAsync(token);
        var registered = definitions.ReadModels.Single(model => model.Type.Identifier == Model);
        JsonNode.DeepEquals(JsonNode.Parse(registered.Schema), JsonNode.Parse(Schema)).ShouldBeTrue();
        registered.Type.Generation.ShouldEqual(1u);
        registered.ContainerName.ShouldEqual(Table);
        registered.Sink.TypeId.ShouldEqual("SQL");
        await Services.Projections.Register(new()
        {
            EventStore = Store,
            Owner = ProjectionOwner.Client,
            FullSet = false,
            Projections =
            [
                new()
                {
                    Identifier = Projection,
                    ReadModel = Model,
                    EventSequenceId = EventSequenceId.Log.Value,
                    IsActive = true,
                    IsRewindable = true,
                    InitialModelState = "{}",
                    AutoMap = AutoMap.Disabled,
                    From = new Dictionary<Contracts.Events.EventType, FromDefinition>
                    {
                        [new() { Id = LegacyIdentifierNamed.EventTypeId, Generation = 1 }] = new()
                        {
                            Key = "$eventSourceId",
                            Properties = new Dictionary<string, string> { ["Name"] = "$event.name" }
                        }
                    }
                }
            ]
        }).WaitAsync(token);
    }

    public async Task<ulong> Append(string id, string name, CancellationToken token)
    {
        var result = await _store.EventLog.Append(id, new LegacyIdentifierNamed(name)).WaitAsync(token);
        result.IsSuccess.ShouldBeTrue();
        result.SequenceNumber.ShouldNotEqual(EventSequenceNumber.Unavailable);
        var completion = await result.WaitForCompletion(TimeSpan.FromSeconds(45)).WaitAsync(token);
        completion.IsSuccess.ShouldBeTrue();
        completion.FailedPartitions.ShouldBeEmpty();
        var stored = await _store.EventLog.GetForEventSourceIdAndEventTypes(id, []).WaitAsync(token);
        var acknowledged = stored.Single(@event => @event.Context.SequenceNumber == result.SequenceNumber);
        ((LegacyIdentifierNamed)acknowledged.Content).Name.ShouldEqual(name);
        return result.SequenceNumber.Value;
    }

    public async Task Read(string id, string name, CancellationToken token)
    {
        var result = await Services.ReadModels.GetInstanceByKey(new()
        {
            EventStore = Store,
            Namespace = EventStoreNamespaceName.Default.Value,
            ReadModelIdentifier = Model,
            EventSequenceId = EventSequenceId.Log.Value,
            ReadModelKey = id
        }).WaitAsync(token);
        var model = JsonNode.Parse(result.ReadModel)!;
        model["ID"]!.GetValue<string>().ShouldEqual(id);
        model["Name"]!.GetValue<string>().ShouldEqual(name);
    }

    public async Task Replay(CancellationToken token)
    {
        var result = await Services.Observers.Replay(new()
        {
            EventStore = Store,
            Namespace = EventStoreNamespaceName.Default.Value,
            ObserverId = Projection,
            EventSequenceId = EventSequenceId.Log.Value
        }).WaitAsync(token);
        Guid.Parse(result.JobId).ShouldNotEqual(Guid.Empty);
    }

    public async Task WaitForReplayToEnd(CancellationToken token)
    {
        // Replay() returns once the observer is Replaying; it leaves that state only after the job has finalized.
        var observer = await Services.Observers.ObserveObservers(new()
            {
                EventStore = Store,
                Namespace = EventStoreNamespaceName.Default.Value
            })
            .Select(observers => observers.SingleOrDefault(_ => _.Id == Projection))
            .FirstAsync(_ => _ is not null && _.RunningState != Contracts.Observation.ObserverRunningState.Replaying)
            .ToTask(token);
        observer!.RunningState.ShouldEqual(Contracts.Observation.ObserverRunningState.Active);
    }

    public async Task WaitForCompletion(ulong tail, CancellationToken token)
    {
        var result = await Services.Observers.WaitForCompletion(new()
        {
            EventStore = Store,
            Namespace = EventStoreNamespaceName.Default.Value,
            EventSequenceId = EventSequenceId.Log.Value,
            TailEventSequenceNumber = tail,
            TimeoutMilliseconds = 45000
        }).WaitAsync(token);
        result.IsSuccess.ShouldBeTrue();
        result.TimedOut.ShouldBeFalse();
        result.OutstandingObservers.ShouldBeEmpty();
        result.FailedPartitions.ShouldBeEmpty();
    }

    public void Dispose() => _client.Dispose();
}
