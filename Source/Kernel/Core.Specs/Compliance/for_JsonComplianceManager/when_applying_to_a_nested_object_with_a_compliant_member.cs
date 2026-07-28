// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_applying_to_a_nested_object_with_a_compliant_member : given.a_value_handler_and_a_type_with_a_compliant_member_on_a_nested_object
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    const string EncryptedValue = "encrypted";

    JsonObject _input;
    JsonObject _result;

    void Establish()
    {
        _input = new JsonObject
        {
            ["id"] = "some-id",
            ["dateOfBirth"] = new JsonObject
            {
                ["dateOfBirth"] = "1815-12-10",
                ["verifiedBy"] = "bankid"
            }
        };

        _valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>()).Returns(_ => Task.FromResult<JsonNode>(JsonValue.Create(EncryptedValue)));
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_encrypt_the_compliant_member() => _result["dateOfBirth"]!["dateOfBirth"]!.GetValue<string>().ShouldEqual(EncryptedValue);
    [Fact] void should_keep_the_non_compliant_sibling() => _result["dateOfBirth"]!["verifiedBy"]!.GetValue<string>().ShouldEqual("bankid");
    [Fact] void should_keep_the_nested_object_shape() => (_result["dateOfBirth"] is JsonObject).ShouldBeTrue();
}
