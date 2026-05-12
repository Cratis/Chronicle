// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_required_nullable_property_is_missing : Specification
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    record EventWithNullableProperty(string Name, DateOnly? StartDate);

    IList<JsonSchemaValidationError> _result;

    void Because()
    {
        var schema = JsonSchema.FromType<EventWithNullableProperty>();
        var content = JsonSerializer.Serialize(new EventWithNullableProperty("Mission", null), _serializerOptions);

        _result = schema.Validate(content);
    }

    [Fact] void should_not_have_validation_errors() => _result.ShouldBeEmpty();
}
