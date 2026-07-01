// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_releasing_a_compliant_list : given.a_value_handler_and_a_type_with_a_compliant_list
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    JsonObject _result;

    void Establish()
    {
        // The handler decrypts the blob back to the original JSON array text (as a string); the manager
        // must re-parse that text into the array the schema expects, not leave it as a raw string.
        _valueHandler.Release(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>())
            .Returns(Task.FromResult<JsonNode>(JsonValue.Create("""[{"criterion":"quality","score":5}]""")));
    }

    async Task Because() => _result = await _manager.Release(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_release_the_list_as_an_array() => (_result[ListPropertyName] is JsonArray).ShouldBeTrue();
    [Fact] void should_release_a_single_element() => _result[ListPropertyName]!.AsArray().Count.ShouldEqual(1);
    [Fact] void should_release_the_element_criterion() => _result[ListPropertyName]![0]!["criterion"]!.GetValue<string>().ShouldEqual("quality");
    [Fact] void should_release_the_element_score() => _result[ListPropertyName]![0]!["score"]!.GetValue<int>().ShouldEqual(5);
}
