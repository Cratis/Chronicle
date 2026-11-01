// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.for_EventCompliance.given;

public class a_stored_generation_with_inherited_protection : Specification
{
    protected const string Identifier = "person-not-source";
    protected EventType _type = new("event", 1);
    protected JsonSchema _own;
    protected JsonSchema _classified;
    protected EventTypeDefinition _definition;
    protected IEventTypeSchemaCache _cache;
    protected EventCompliance _compliance;
    protected JsonSchemaMetadataManager _manager;
    protected InMemoryEncryptionKeyStorage _keys;
    protected IEventTypesStorage _eventTypes;
    protected ExpandoObjectConverter _converter;
    protected JsonObject _plaintext = new() { ["name"] = "historical personal name", ["secret"] = "historical unencrypted secret" };

    async Task Establish()
    {
        _own = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string"},"secret":{"type":"string"}}}""");
        _classified = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},"secret":{"type":"string","security":[{"metadataType":"EncryptedSubject","details":""}]}}}""");
        _definition = new(_type.Id, EventTypeOwner.Client, false, [new(1, _own), new(2, _classified)], []);
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _eventTypes.GetDefinition(_type.Id).Returns(_ => _definition);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore("store").EventTypes.Returns(_eventTypes);
        _cache = new EventTypeSchemaCache(storage);
        _keys = new();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
            new PIICompliancePropertyValueHandler(provisioner, _keys, encryption),
            new EncryptedSubjectValueHandler(provisioner, _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new(new TypeFormats());
        _compliance = new(_manager, _converter, _cache);
    }

    protected AppendedEvent EventWith(JsonObject content) => new(
        EventContext.From("store", "namespace", _type, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0UL, CorrelationId.New(), subject: Identifier),
        _converter.ToExpandoObject(content, _own));
}
