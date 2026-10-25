// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status.given;

public class a_manager : Specification
{
    protected const string Identifier = "original-subject";
    protected InMemoryEncryptionKeyStorage _keys;
    protected JsonSchemaMetadataManager _manager;
    protected JsonSchema _schema;
    protected JsonObject _source;
    protected JsonObject _protected;
    protected StatusedRelease _result;

    void Establish()
    {
        var encryption = new Encryption();
        _keys = new();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keys, encryption);
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, _keys, encryption), new EncryptedGlobalValueHandler(provisioner, _keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
    }

    protected async Task ProtectAndErase()
    {
        _protected = await _manager.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _source);
        await _keys.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
        await _keys.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
    }
}
