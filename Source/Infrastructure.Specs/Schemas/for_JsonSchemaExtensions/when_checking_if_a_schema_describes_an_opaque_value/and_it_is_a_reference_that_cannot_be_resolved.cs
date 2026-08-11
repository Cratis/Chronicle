// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_an_opaque_value;

/// <summary>
/// A reference that resolves to nothing declares no properties, but for the opposite reason an opaque value
/// does: its declaration exists and could not be reached. Treating it as opaque would let whatever reads it
/// skip a value the schema does mean to describe, so it has to stay loud.
/// </summary>
public class and_it_is_a_reference_that_cannot_be_resolved : Specification
{
    const string Schema = """{"type":"object","$ref":"#/$defs/NotThere"}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Schema).DescribesOpaqueValue();

    [Fact] void should_not_describe_an_opaque_value() => _result.ShouldBeFalse();
}
