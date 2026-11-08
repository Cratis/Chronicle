// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.given;

public class an_erased_key_behind_a_warm_cache : Specification
{
    protected InMemoryEncryptionKeyStorage _inner;
    protected CacheEncryptionKeyStorage _cache;
    protected Encryption _encryption;
    protected ManagedEncryptionKeyProvisioner _provisioner;
    protected IJsonSchemaMetadataValueHandler _handler;
    protected JsonNode _protected;
    protected JsonNode _result;
    protected JsonNode _tried;

    void Establish()
    {
        _inner = new();
        _cache = new(_inner);
        _encryption = new();
        _provisioner = new(_cache, _encryption);
    }

    protected async Task ProtectAndErase(EventStoreName store, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        _protected = await _handler.Apply("store", "ns", "subject", JsonValue.Create("confidential"));
        (await _handler.Release("store", "ns", "subject", _protected)).ToString().ShouldEqual("confidential");
        await _inner.RecordErasureFor(store, ns, identifier);
        await _inner.DeleteFor(store, ns, identifier);
    }

    protected async Task ReleaseValues()
    {
        _result = await _handler.Release("store", "ns", "subject", _protected);
        _tried = await _handler.TryRelease("store", "ns", "subject", _protected);
    }
}
