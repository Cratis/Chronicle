// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Specs.Compliance.given;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_round_tripping_non_string_security_values : Specification
{
    readonly protected_values _values = new();
    BsonDocument _stored;
    JsonObject _released;

    async Task Because()
    {
        var converter = new ExpandoObjectConverter(new Schemas.TypeFormats());
        _stored = converter.ToBsonDocument(await _values.Encrypt(), _values.Schema);
        _released = await _values.Release(converter.ToExpandoObject(_stored, _values.Schema));
    }

    [Fact] void should_store_ciphertext_for_every_scope_and_type() => _stored.Elements.All(element => element.Value.IsString).ShouldBeTrue();
    [Fact] void should_restore_every_original_json_type_and_value() => JsonNode.DeepEquals(_released, _values.Plaintext).ShouldBeTrue();
}
