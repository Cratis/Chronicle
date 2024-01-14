// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Aksio.Cratis.Kernel.Compliance.for_JsonComplianceManager;

public class when_releasing_with_applicable_value_handler : given.a_value_handler_and_a_type_with_one_property
{
    const string identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    const string changed_value = "FortyTwo";
    JsonObject result;
    JsonNode property_value;

    void Establish()
    {
        property_value = JsonValue.Create(changed_value);
        value_handler.Setup(_ => _.Release(string.Empty, string.Empty, identifier, IsAny<JsonNode>())).Returns(Task.FromResult(property_value));
    }

    async Task Because() => result = await manager.Release(string.Empty, string.Empty, schema, identifier, input);

    [Fact] void should_return_instance_with_altered_property() => result[property_name].ShouldEqual(property_value);
}
