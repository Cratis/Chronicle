// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_applying_to_a_compliant_list : given.a_value_handler_and_a_type_with_a_compliant_list
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    JsonObject _result;

    void Establish()
    {
        // Apply receives the plaintext array and blob-encrypts it to a single ciphertext string; the
        // re-parse must NOT run on apply, or the ciphertext would be corrupted back into an array.
        _input = new JsonObject
        {
            [ListPropertyName] = new JsonArray(new JsonObject { ["criterion"] = "quality", ["score"] = 5 })
        };

        _valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>())
            .Returns(Task.FromResult<JsonNode>(JsonValue.Create("cipher-text-blob")));
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_store_the_list_as_a_ciphertext_string() => (_result[ListPropertyName] is JsonValue).ShouldBeTrue();
    [Fact] void should_not_reparse_the_ciphertext_into_an_array() => (_result[ListPropertyName] is JsonArray).ShouldBeFalse();
}
