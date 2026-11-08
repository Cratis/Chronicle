// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Imports;
using Microsoft.Extensions.Options;
using ImportsStorage = Cratis.Chronicle.Storage.InMemory.Imports.ImportStorage;

namespace Cratis.Chronicle.Imports.for_ImportStager.given;

public class a_stager : Specification
{
    protected ImportStager _stager;
    protected ImportKey _key;
    protected IImport _import;
    protected ImportsStorage _imports;
    protected IEventTypesStorage _eventTypes;
    protected ImportChunkStagingResult _result;
    protected ChronicleOptions _options;

    void Establish()
    {
        _key = new("store", "namespace", ImportId.New());
        _imports = new();
        _options = new();
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _eventTypes.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(call => call.Arg<EventTypeId>().Value == "known");
        _eventTypes.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(new EventType("known", 1), EventTypeOwner.Server, EventTypeSource.Code, JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""")));
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(_key.EventStore).EventTypes.Returns(_eventTypes);
        storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).Imports.Returns(_imports);

        _import = Substitute.For<IImport>();
        _import.EnsureAcceptsChunks().Returns(new ImportOperationResult(new ImportRecord(_key.ImportId, EventSequenceId.Log, OnInvalidEvent.Reject, DateTimeOffset.UtcNow)));
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IImport>((string)_key, null).Returns(_import);
        _stager = new(grainFactory, storage, Options.Create(_options));
    }

    protected static ImportEvent Event(string eventType = "known", string content = """{"name":"Alice"}""", string? idempotencyKey = null) => new(
        "source",
        string.Empty,
        string.Empty,
        string.Empty,
        new Sequences.EventType(eventType, 1, false),
        content,
        ["tag"],
        Occurred: DateTimeOffset.UnixEpoch,
        IdempotencyKey: idempotencyKey,
        SourceSequenceNumber: 42);

    protected Task<ImportChunkStagingResult> Stage(int chunkIndex, params ImportEvent[] events) =>
        _stager.Stage(_key, chunkIndex, chunkIndex * 100, events, events.Select(_ => Identity.System).ToArray());
}
