// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_a_geospatial_value;

/// <summary>
/// A nullable value carries its nullability as a trailing marker on the format, which must not change what the
/// format identifies.
/// </summary>
public class and_it_is_a_nullable_geospatial_leaf : Specification
{
    const string Schema = """{"type":"object","format":"point?","title":"Point"}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Schema).DescribesGeospatialValue();

    [Fact] void should_describe_a_geospatial_value() => _result.ShouldBeTrue();
}
