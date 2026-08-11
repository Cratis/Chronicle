// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_an_opaque_value;

/// <summary>
/// A base type with registered derived types is emitted without properties so the derived payload and its
/// type discriminator round-trip verbatim - the concrete members are not knowable from the base's schema.
/// </summary>
public class and_it_is_a_polymorphic_open_object : Specification
{
    const string Schema = """{"type":"object","title":"Shape"}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Schema).DescribesOpaqueValue();

    [Fact] void should_describe_an_opaque_value() => _result.ShouldBeTrue();
}
