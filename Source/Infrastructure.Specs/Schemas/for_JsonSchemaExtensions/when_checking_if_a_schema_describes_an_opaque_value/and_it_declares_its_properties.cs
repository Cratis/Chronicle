// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_an_opaque_value;

public class and_it_declares_its_properties : Specification
{
    const string Schema = """{"type":"object","properties":{"city":{"type":"string"}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Schema).DescribesOpaqueValue();

    [Fact] void should_not_describe_an_opaque_value() => _result.ShouldBeFalse();
}
