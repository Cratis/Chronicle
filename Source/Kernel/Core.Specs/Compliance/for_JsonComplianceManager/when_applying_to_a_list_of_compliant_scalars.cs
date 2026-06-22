// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_applying_to_a_list_of_compliant_scalars : given.a_value_handler_and_a_type_with_a_list_of_compliant_scalars
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    const string EncryptedValue = "encrypted";

    JsonObject _input;
    JsonObject _result;

    void Establish()
    {
        _input = new JsonObject
        {
            ["emails"] = new JsonArray("one@cratis.io", "two@cratis.io")
        };

        _valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create(EncryptedValue)));
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_encrypt_every_element() => _result["emails"]!.AsArray().Select(_ => _!.GetValue<string>()).ToArray().ShouldEqual([EncryptedValue, EncryptedValue]);
    [Fact] void should_invoke_the_handler_for_each_element() => _valueHandler.Received(2).Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>());
}
