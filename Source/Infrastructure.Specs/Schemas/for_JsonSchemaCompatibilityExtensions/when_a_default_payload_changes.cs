// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_a_default_payload_changes : Specification
{
    bool _compatible;

    void Because() => _compatible = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"default":{"title":"old","compliance":[]}}}}""")
        .IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"value":{"default":{"title":"new","compliance":[]}}}}"""));

    [Fact] void should_not_erase_data_as_schema_metadata() => _compatible.ShouldBeFalse();
}
