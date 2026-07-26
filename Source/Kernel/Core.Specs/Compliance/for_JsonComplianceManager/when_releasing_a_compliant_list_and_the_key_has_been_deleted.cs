// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_releasing_a_compliant_list_and_the_key_has_been_deleted : given.a_value_handler_and_a_type_with_a_compliant_list
{
    const string Identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    JsonObject _result;

    void Establish()
    {
        // After the subject's encryption key has been crypto-shredded (GDPR right-to-erasure), the handler
        // surfaces the erased value as an empty string. The manager must round-trip that into an empty array
        // for a coarse [PII] collection instead of throwing while re-parsing the empty released value.
        _valueHandler.Release(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>())
            .Returns(Task.FromResult<JsonNode>(JsonValue.Create(string.Empty)));
    }

    async Task Because() => _result = await _manager.Release(string.Empty, string.Empty, _schema, Identifier, _input);

    [Fact] void should_release_the_list_as_an_array() => (_result[ListPropertyName] is JsonArray).ShouldBeTrue();
    [Fact] void should_release_an_empty_array() => _result[ListPropertyName]!.AsArray().Count.ShouldEqual(0);
}
