// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_a_default_only_refinement_introduces_a_constraint : Specification
{
    bool _compatible;

    void Because() => _compatible = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null}}}""")
        .IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null,"type":"number","format":"decimal","minimum":0}}}"""));

    [Fact] void should_require_a_new_generation() => _compatible.ShouldBeFalse();
}
