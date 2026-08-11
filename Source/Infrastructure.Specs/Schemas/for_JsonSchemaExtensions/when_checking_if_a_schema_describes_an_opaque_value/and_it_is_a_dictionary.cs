// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_an_opaque_value;

/// <summary>
/// A dictionary declares the shape of its values, never their names - the keys in the document are data.
/// </summary>
public class and_it_is_a_dictionary : Specification
{
    const string Schema = """{"type":"object","additionalProperties":{"type":"string"}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Schema).DescribesOpaqueValue();

    [Fact] void should_describe_an_opaque_value() => _result.ShouldBeTrue();
}
