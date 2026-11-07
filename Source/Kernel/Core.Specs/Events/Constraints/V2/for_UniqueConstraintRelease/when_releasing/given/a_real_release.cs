// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing.given;

/// <summary>
/// A release wired to the real metadata manager, PII handler and in-memory key storage.
/// </summary>
public class a_real_release : a_factory
{
    protected const string Subject = "source";
    protected InMemoryEncryptionKeyStorage _keys;
    protected JsonSchemaMetadataManager _manager;
    protected JsonSchema _ownSchema;
    protected JsonSchema _protectionSchema;

    void Establish()
    {
        var encryption = new Encryption();
        _keys = new();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        _ownSchema = JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string"}}}""");
        _protectionSchema = JsonSchema.FromJson("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}""");
        _schemas.GetFor("event", 2).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Server, EventTypeSource.Code, _ownSchema));
        _schemaCache.ReturnsForAll<ValueTask<JsonSchema>>(_ => ValueTask.FromResult(_protectionSchema));
        _factory = new(_schemas, _schemaCache, _manager, new UniqueConstraintComponentsBuilder(new ExpandoObjectConverter(new TypeFormats())));
        _definition = new("event", ["name"]);
    }

    protected async Task StoreProtectedName(string name)
    {
        var protectedContent = await _manager.Apply(Store, Namespace, _protectionSchema, Subject, new JsonObject { ["name"] = name });
        SetContent(protectedContent.ToJsonString());
    }

    protected Task Erase() => EraseSubject(recordErasure: true);

    protected Task DeleteKeyWithoutErasure() => EraseSubject(recordErasure: false);

    async Task EraseSubject(bool recordErasure)
    {
        if (recordErasure)
        {
            await _keys.RecordErasureFor(Store, Namespace, Subject);
        }

        await _keys.DeleteFor(Store, Namespace, Subject);
    }
}
