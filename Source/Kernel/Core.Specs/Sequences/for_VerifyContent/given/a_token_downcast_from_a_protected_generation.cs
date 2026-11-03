// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_token_downcast_from_a_protected_generation : a_stored_event
{
    protected const string Token = "personal-token";
    protected InMemoryEncryptionKeyStorage _keys;
    protected JsonSchema _plain;
    protected JsonSchema _protected;
    protected EventTypeDefinition _definition;

    async Task Establish()
    {
        _keys = new();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _plain = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"token":{"type":"string"}}}""");
        _protected = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"token":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _definition = new(
            "event",
            EventTypeOwner.Client,
            false,
            [new(1, _plain), new(2, _plain), new(3, _protected)],
            [new(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())]);
        var eventTypes = _storage.GetEventStore("store").EventTypes;
        eventTypes.Configure().GetDefinition("event").Returns(_definition);
        eventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, _plain));
        eventTypes.HasFor("event", 2U).Returns(true);
        eventTypes.GetFor("event", 2U).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Client, EventTypeSource.Code, _plain));
        eventTypes.HasFor("event", 3U).Returns(true);
        eventTypes.GetFor("event", 3U).Returns(new EventTypeSchema(new("event", 3), EventTypeOwner.Client, EventTypeSource.Code, _protected));
        var content = new JsonObject { ["token"] = Token };
        var protectedContent = await _manager.Apply("store", "tenant", _protected, "person", content);
        _stored = _stored with
        {
            Context = _stored.Context with { EventType = new("event", 3), AppendedGeneration = 3, Subject = "person", EventStore = "store", Namespace = "tenant" },
            GenerationalContent = new Dictionary<int, string> { [1] = protectedContent.ToJsonString(), [2] = protectedContent.ToJsonString(), [3] = protectedContent.ToJsonString() }
        };
        _command = _command with { EventType = new("event", 3, false), Content = content.ToJsonString() };
    }
}
