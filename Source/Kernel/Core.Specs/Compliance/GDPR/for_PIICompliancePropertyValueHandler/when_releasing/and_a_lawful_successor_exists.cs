// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler;

public class and_a_lawful_successor_exists : Specification
{
    PIICompliancePropertyValueHandler _handler;
    JsonNode _value;
    JsonNode _result;

    async Task Establish()
    {
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        _handler = new(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", JsonValue.Create("old value"));
        await keys.RecordErasureFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner");
        await keys.DeleteFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner");
        await keys.AllowNewKeyFor(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner");
        _value = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", JsonValue.Create("new value"));
    }

    async Task Because() => _result = await _handler.Release(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, "owner", _value);

    [Fact] void should_release_the_successor_value() => _result.ToString().ShouldEqual("new value");
}
