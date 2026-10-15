// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_stored_event : Specification
{
    protected ExpandoObjectConverter _converter;
    protected IStorage _storage;
    protected IEventCursor _cursor;
    protected JsonSchemaMetadataManager _manager;
    protected Concepts.Events.AppendedEvent _stored;
    protected VerifyContent _command;
    protected ContentVerification _result;

    protected Task<ContentVerification> Verify() => _command.Handle(_storage, _manager, _converter, new EventTypeMigrations(_storage, _converter));

    void Establish()
    {
        _converter = new(new TypeFormats());
        _storage = Substitute.For<IStorage>();
        _cursor = Substitute.For<IEventCursor>();
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(), NullLogger<JsonSchemaMetadataManager>.Instance);
        _command = new("store", "tenant", "log", EventSequenceNumber.First, new("event", 1, false), "{\"value\":42}");
        _stored = Concepts.Events.AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new("event", 2), EventSequenceNumber.First) with
        {
            GenerationalContent = new Dictionary<int, string> { [1] = "{\"value\":42}" }
        };
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
            .GetRange(EventSequenceNumber.First, EventSequenceNumber.First).Returns(_cursor);
        var serializer = new Storage.InMemory.EventSequences.EventSequenceStorage("store", "tenant", "log", new Storage.InMemory.Identities.IdentityStorage());
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
            .SerializeContentForVerification(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => serializer.SerializeContentForVerification(call.Arg<ExpandoObject>(), call.Arg<JsonSchema>()));
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log").SupportsRevisionTracking.Returns(true);
        _storage.GetEventStore("store").EventTypes.GetDefinition("event").Returns(async _ => new EventTypeDefinition(
            "event",
            EventTypeOwner.Client,
            false,
            [new EventTypeGenerationDefinition(1, (await _storage.GetEventStore("store").EventTypes.GetFor("event", 1U)).Schema)],
            []));
        _cursor.MoveNext().Returns(true);
        _cursor.Current.Returns(_ => [_stored]);
        _storage.GetEventStore("store").EventTypes.HasFor("event", 1U).Returns(true);
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()));
    }
}
