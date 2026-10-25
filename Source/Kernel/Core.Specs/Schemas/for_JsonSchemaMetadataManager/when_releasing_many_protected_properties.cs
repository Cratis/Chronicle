// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_releasing_many_protected_properties : given.real_pii_protection
{
    JsonObject _afterErasure;

    async Task Establish()
    {
        _schema = new() { Type = JsonObjectType.Object };
        _plaintext = new();
        for (var index = 0; index < 100; index++)
        {
            var name = $"property{index}";
            _schema.Properties[name] = new(name, new JsonObject(), _schema) { Type = JsonObjectType.String };
            Mark(_schema.Properties[name]);
            _plaintext[name] = "protected value";
        }

        await Protect();
        _keys.ClearReceivedCalls();
    }

    async Task Because()
    {
        _result = await _manager.Release("store", "tenant", _schema, "subject", _protected);
        _keys.GetErasureForRelease("store", "tenant", "subject").Returns(new EncryptionKeyErasure(1, [], false));
        _afterErasure = await _manager.Release("store", "tenant", _schema, "subject", _protected);
    }

    [Fact] void should_read_one_fence_per_document_not_per_property() => _keys.Received(2).GetErasureForRelease("store", "tenant", "subject");
    [Fact] void should_release_all_properties() => _result.All(property => property.Value.ToString() == "protected value").ShouldBeTrue();
    [Fact] void should_observe_erasure_in_the_next_document() => _afterErasure.All(property => property.Value.ToString().Length == 0).ShouldBeTrue();
}
