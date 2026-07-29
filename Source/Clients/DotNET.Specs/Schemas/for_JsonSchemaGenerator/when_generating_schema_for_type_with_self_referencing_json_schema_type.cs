// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// A <see cref="JsonSchemaTypeAttribute"/> pointing at the type it adorns would substitute the type's schema with
/// its own schema forever, so it is rejected with a message naming the offending type instead of overflowing the stack.
/// </summary>
public class when_generating_schema_for_type_with_self_referencing_json_schema_type : given.a_json_schema_generator
{
    [JsonSchemaType(typeof(Beacon))]
    record Beacon(string Signal);

    Exception _result;

    void Because() => _result = Catch.Exception(() => _generator.Generate(typeof(Beacon)));

    [Fact] void should_fail_with_self_referencing_json_schema_type() => _result.ShouldBeOfExactType<SelfReferencingJsonSchemaType>();
}
