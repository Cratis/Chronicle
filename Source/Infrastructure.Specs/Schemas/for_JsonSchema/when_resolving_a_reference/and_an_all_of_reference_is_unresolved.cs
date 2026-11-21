// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class and_an_all_of_reference_is_unresolved : Specification
{
    JsonSchema _schema;
    JsonSchema _actual;

    void Establish() => _schema = JsonSchema.FromJson("""{"allOf":[{"$ref":"missing"}],"title":"original"}""");

    void Because() => _actual = _schema.ActualSchema;

    [Fact] void should_keep_the_original_schema_as_the_fallback() => _actual.ShouldEqual(_schema);
}
