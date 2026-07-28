// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_round_tripping_a_compliant_value_object : given.a_value_handler_and_a_type_with_a_compliant_value_object
{
    JsonObject _input;
    JsonObject _applied;
    JsonObject _released;

    void Establish() =>
        _input = new JsonObject
        {
            ["id"] = "some-id",
            ["dateOfBirth"] = new JsonObject
            {
                ["dateOfBirth"] = "1815-12-10",
                ["verifiedBy"] = "bankid"
            }
        };

    async Task Because()
    {
        _applied = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);
        _released = await _manager.Release(string.Empty, string.Empty, _schema, Identifier, _applied);
    }

    [Fact] void should_leave_no_plaintext_in_the_applied_payload() => _applied.ToJsonString().ShouldNotContain("1815-12-10");
    [Fact] void should_handle_the_value_object_exactly_once() => _valueHandler.Received(1).Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>());
    [Fact] void should_restore_the_object_shape_on_release() => (_released["dateOfBirth"] is JsonObject).ShouldBeTrue();
    [Fact] void should_round_trip_the_nested_value() => _released["dateOfBirth"]!["dateOfBirth"]!.GetValue<string>().ShouldEqual("1815-12-10");
    [Fact] void should_round_trip_the_nested_sibling() => _released["dateOfBirth"]!["verifiedBy"]!.GetValue<string>().ShouldEqual("bankid");
}
