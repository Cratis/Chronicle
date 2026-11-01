// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public abstract class a_second_writer_parked_before_its_update(ReplicaSetMongoDBFixture fixture) : a_replica_set_event_sequence_storage(fixture)
{
    protected EventSequenceStorage _secondWriter;
    protected int _armed;
    protected TaskCompletionSource _parked;
    protected ManualResetEventSlim _release;
    protected bool _gateTimedOut;

    readonly ReplicaSetMongoDBFixture _fixture = fixture;
    MongoClient _secondClient;

    void Establish()
    {
        _parked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _release = new ManualResetEventSlim();
        var settings = MongoClientSettings.FromConnectionString(_fixture.ConnectionString);
        settings.ClusterConfigurator = builder => builder.Subscribe<CommandStartedEvent>(OnCommandStarted);
        _secondClient = new MongoClient(settings);
        var database = _secondClient.GetDatabase(_collection.Database.DatabaseNamespace.DatabaseName);
        var namespaceDatabase = Substitute.For<IEventStoreNamespaceDatabase>();
        namespaceDatabase.Client.Returns(_secondClient);
        namespaceDatabase.GetEventSequenceCollectionFor(Arg.Any<EventSequenceId>()).Returns(database.GetCollection<Event>("event-log"));

        _secondWriter = new EventSequenceStorage(
            "test-store",
            "test-namespace",
            EventSequenceId.Log,
            namespaceDatabase,
            new EventConverter("test-store", "test-namespace", _eventTypesStorage, _identityStorage, _expandoObjectConverter),
            _eventTypesStorage,
            _identityStorage,
            _expandoObjectConverter,
            new JsonSerializerOptions(),
            Substitute.For<ILogger<EventSequenceStorage>>());
    }

    void Destroy()
    {
        _release.Set();
        _secondClient.Dispose();
        _release.Dispose();
    }

    void OnCommandStarted(CommandStartedEvent command)
    {
        if ((string.Equals(command.CommandName, "update", StringComparison.Ordinal) || string.Equals(command.CommandName, "bulkWrite", StringComparison.Ordinal)) &&
            Interlocked.Exchange(ref _armed, 0) == 1)
        {
            _parked.TrySetResult();
            _gateTimedOut = !_release.Wait(TimeSpan.FromSeconds(30));
        }
    }
}
