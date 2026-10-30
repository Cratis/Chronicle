// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_identified_by_property_is_not_a_plain_string : given.a_projection_with_source_keys
{
    [Theory]
    [InlineData("{\"type\":\"boolean\"}")]
    [InlineData("{\"type\":\"array\",\"items\":{\"type\":\"string\"}}")]
    [InlineData("{\"type\":\"object\"}")]
    [InlineData("{\"type\":[\"string\",\"null\"]}")]
    [InlineData("{\"type\":\"string\",\"oneOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}")]
    [InlineData("{\"type\":\"string\",\"anyOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}")]
    [InlineData("{\"type\":\"string\",\"allOf\":[{\"type\":\"string\",\"format\":\"guid\"}]}")]
    [InlineData("{\"type\":\"string\",\"format\":\"unknown\"}")]
    [InlineData("{}")]
    public async Task should_not_classify_the_projection_as_event_source_keyed(string propertySchema)
    {
        var schema = await JsonSchema.FromJsonAsync("{\"type\":\"object\",\"properties\":{\"Id\":" + propertySchema + "}}");
        _readModel.GetSchemaForLatestGeneration().Properties["Id"] = schema.Properties["Id"];
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

        _projection.IsEventSourceKeyed.ShouldBeFalse();
    }
}
