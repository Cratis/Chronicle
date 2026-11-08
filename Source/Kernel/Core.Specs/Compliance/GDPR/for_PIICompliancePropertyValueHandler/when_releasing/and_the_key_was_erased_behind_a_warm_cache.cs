// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class and_the_key_was_erased_behind_a_warm_cache : Specification
{
    PIICompliancePropertyValueHandler _handler;
    JsonNode _encrypted;
    JsonNode _result;
    const string Identifier = "erased-owner";

    async Task Establish()
    {
        var inner = new InMemoryEncryptionKeyStorage();
        var cache = new CacheEncryptionKeyStorage(inner);
        var encryption = new Encryption();
        _handler = new(new ManagedEncryptionKeyProvisioner(cache, encryption), cache, encryption);
        _encrypted = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, JsonValue.Create("protected value"));
        (await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _encrypted)).ToString().ShouldEqual("protected value");
        await inner.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
        await inner.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier);
    }

    async Task Because() => _result = await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, _encrypted);

    [Fact] void should_release_empty() => _result.ToString().ShouldEqual(string.Empty);
}
