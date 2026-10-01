// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR.for_PIICompliancePropertyValueHandler.when_applying_an_empty_value;

public class and_it_is_an_empty_container : given.a_property_handler
{
    JsonNode _object;
    JsonNode _array;

    async Task Because()
    {
        _object = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, new JsonObject());
        _array = await _handler.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, Identifier, new JsonArray());
    }

    [Fact] void should_keep_the_empty_object() => _object.ShouldBeOfExactType<JsonObject>();
    [Fact] void should_keep_the_empty_array() => _array.ShouldBeOfExactType<JsonArray>();
    [Fact] void should_not_encrypt() => _encryption.DidNotReceive().Encrypt(Arg.Any<byte[]>(), Arg.Any<EncryptionKey>());
}
