// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Compliance.for_ComplianceService.given;

public class a_compliance_service : Specification
{
    protected const string SensitiveValue = "Private contact name";
    protected readonly EventStoreName event_store = "service-store";
    protected readonly EventStoreNamespaceName event_store_namespace = "service-namespace";
    protected readonly EncryptionKeyIdentifier identifier = "service-subject";
    protected IEncryptionKeyStorage storage;
    protected ReleaseRequest request;
    protected ReleaseResponse result;
    private protected ComplianceService service;

    async Task Establish()
    {
        storage = new Storage.InMemory.Compliance.EncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(storage, encryption);
        var handler = new PIICompliancePropertyValueHandler(provisioner, storage, encryption);
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        var content = await manager.Apply(event_store, event_store_namespace, schema, identifier, new JsonObject { ["name"] = SensitiveValue });
        request = new ReleaseRequest
        {
            EventStore = event_store,
            Namespace = event_store_namespace,
            Subject = identifier,
            Schema = schema.ToJson(),
            Payload = content.ToJsonString(),
            Strict = true
        };
        service = new ComplianceService(Substitute.For<IGrainFactory>(), manager, NullLogger<ComplianceService>.Instance);
    }

    protected async Task EraseKey()
    {
        await storage.RecordErasureFor(event_store, event_store_namespace, identifier);
        await storage.DeleteFor(event_store, event_store_namespace, identifier);
    }
}
