// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_retaining_the_more_precise_schema : Specification
{
    JsonSchema _stored;
    JsonSchema _result;

    void Establish() => _stored = JsonSchema.FromJson("""{"type":"object","properties":{"nested":{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null}}}}}""");

    void Because() => _result = _stored.MorePrecise(JsonSchema.FromJson("""{"type":"object","properties":{"nested":{"type":"object","properties":{"amount":{"default":null}}}}}"""));

    [Fact] void should_keep_the_typed_nested_property() => _result.ToJson().ShouldEqual(_stored.ToJson());
}
