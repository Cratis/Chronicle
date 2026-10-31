// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_ProtectedEventTypeMigrations.given;

public class a_token_protected_only_in_the_latest_generation : Specification
{
    protected const string Token = "eyJhbGciOiJIUzI1NiJ9.personal-token.signature";
    protected const string Subject = "subject";
    protected static readonly EventStoreName Store = "store";
    protected static readonly EventStoreNamespaceName Namespace = "namespace";
    protected EventType _eventType = new("token-issued", 3);
    protected IStorage _storage;
    protected IEventTypesStorage _eventTypes;
    protected EventTypeMigrations _migrations;
    private protected ProtectedEventTypeMigrations _sut;
    protected EventTypeDefinition _definition;
    protected JsonSchema[] _schemas;
    protected InMemoryEncryptionKeyStorage _keys;
    protected JsonSchemaMetadataManager _manager;
    protected ExpandoObjectConverter _converter;
    protected JsonObject _plaintext;
    protected JsonObject _protectedContent;
    protected ExpandoObject _protectedSource;
    protected IDictionary<EventTypeGeneration, ExpandoObject> _migrated;

    async Task Establish()
    {
        _keys = new();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new(new TypeFormats());
        _schemas = [
            await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"token":{"type":"string"}}}"""),
            await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"token":{"type":"string"}}}"""),
            await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"token":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""")
        ];
        _definition = new(
            _eventType.Id,
            EventTypeOwner.None,
            false,
            [new(1, _schemas[0]), new(2, _schemas[1]), new(3, _schemas[2])],
            [new(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject(), new JsonObject())]);
        _storage = Substitute.For<IStorage>();
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(Store).EventTypes.Returns(_eventTypes);
        _eventTypes.GetDefinition(_eventType.Id).Returns(_definition);
        for (var index = 0; index < _schemas.Length; index++)
        {
            var type = new EventType(_eventType.Id, (uint)(index + 1));
            _eventTypes.GetFor(type.Id, type.Generation).Returns(new EventTypeSchema(type, EventTypeOwner.Server, EventTypeSource.Code, _schemas[index]));
        }

        _migrations = new(_storage, _converter);
        _sut = new(_eventTypes, _migrations, _manager, _converter);
        _plaintext = new JsonObject { ["token"] = Token };
        _protectedContent = await _manager.Apply(Store, Namespace, _schemas[2], Subject, _plaintext);
        _protectedSource = _converter.ToExpandoObject(_protectedContent, _schemas[2]);
    }

    protected string StoredGeneration(uint generation) => _converter.ToJsonObject(_migrated[(EventTypeGeneration)generation], _schemas[generation - 1]).ToJsonString();
}
