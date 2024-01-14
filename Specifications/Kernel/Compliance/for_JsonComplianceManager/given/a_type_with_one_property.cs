// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Aksio.Cratis.Kernel.Compliance.for_JsonComplianceManager.given;

public class a_type_with_one_property : Specification
{
    record TypeHoldingValue(int Something);
    protected const string property_name = nameof(TypeHoldingValue.Something);
    protected const int value_in_type = 42;
    protected JsonSchema schema;
    protected JsonObject input;

    void Establish()
    {
        var instance = new TypeHoldingValue(value_in_type);
        var settings = new JsonSchemaGeneratorSettings();
        var generator = new JsonSchemaGenerator(settings);
        schema = generator.Generate(instance.GetType());
        input = new JsonObject
        {
            [property_name] = value_in_type
        };
    }
}
