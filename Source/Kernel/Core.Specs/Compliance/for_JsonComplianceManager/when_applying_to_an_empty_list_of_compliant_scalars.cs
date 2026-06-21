// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_applying_to_an_empty_list_of_compliant_scalars : given.a_value_handler_and_a_type_with_a_list_of_compliant_scalars
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";

    JsonObject _input;
    JsonObject _result;

    void Establish() => _input = new JsonObject { ["emails"] = new JsonArray() };

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_keep_the_array_empty() => _result["emails"]!.AsArray().Count.ShouldEqual(0);
    [Fact] void should_not_invoke_the_handler() => _valueHandler.DidNotReceiveWithAnyArgs().Apply(default!, default!, default!, default!);
}
