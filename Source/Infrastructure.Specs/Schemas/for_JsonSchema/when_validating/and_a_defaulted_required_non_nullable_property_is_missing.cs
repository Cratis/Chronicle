// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_a_defaulted_required_non_nullable_property_is_missing : Specification
{
    readonly JsonObject _content = new();
    IList<JsonSchemaValidationError> _errors;

    void Because() => _errors = JsonSchema.FromJson(
        """{"type":"object","required":["count"],"properties":{"count":{"type":"integer","default":1}}}""").Validate(_content);

    [Fact] void should_reject_an_older_payload() => _errors.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.PropertyRequired);
    [Fact] void should_identify_the_missing_property() => _errors.Single().Path.ShouldEqual("count");
    [Fact] void should_not_materialize_the_default() => _content.ContainsKey("count").ShouldBeFalse();
}
