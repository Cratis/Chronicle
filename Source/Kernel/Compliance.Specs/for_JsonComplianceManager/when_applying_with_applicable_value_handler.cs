// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager;

public class when_applying_with_applicable_value_handler : given.a_value_handler_and_a_type_with_one_property
{
    const string _identifier = "9ae5067b-2920-4c97-a263-efe35bec2b43";
    const string _changedValue = "FortyTwo";
    JsonObject _result;
    JsonNode _propertyValue;

    void Establish()
    {
        _propertyValue = JsonValue.Create(_changedValue);
        _valueHandler.Apply(string.Empty, string.Empty, _identifier, Arg.Any<JsonNode>()).Returns(Task.FromResult(_propertyValue));
    }

    async Task Because() => _result = await _manager.Apply(string.Empty, string.Empty, schema, _identifier, input);

    [Fact] void should_return_instance_with_altered_property() => _result[property_name].ShouldEqual(_propertyValue);
}
