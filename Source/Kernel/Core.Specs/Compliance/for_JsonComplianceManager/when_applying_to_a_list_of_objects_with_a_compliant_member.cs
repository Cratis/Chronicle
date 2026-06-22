// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_applying_to_a_list_of_objects_with_a_compliant_member : given.a_value_handler_and_a_type_with_a_list_of_objects_with_a_compliant_member
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    const string EncryptedValue = "encrypted";

    JsonObject _input;
    JsonObject _result;

    void Establish()
    {
        _input = new JsonObject
        {
            ["contacts"] = new JsonArray(
                new JsonObject { ["email"] = "a@cratis.io" },
                new JsonObject { ["email"] = "b@cratis.io" })
        };

        _valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create(EncryptedValue)));
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_encrypt_the_compliant_member_of_each_element() => _result["contacts"]!.AsArray().Select(_ => _!["email"]!.GetValue<string>()).ToArray().ShouldEqual([EncryptedValue, EncryptedValue]);
}
