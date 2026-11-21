// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_a_ref_chain : Specification
{
    JsonSchema _schema;
    JsonSchema _actual;

    void Establish() => _schema = JsonSchema.FromJson("""
        {
          "type":"object","properties":{"value":{"$ref":"#/$defs/A"}},
          "$defs":{"A":{"$ref":"#/$defs/B"},"B":{"$ref":"#/$defs/C"},"C":{"type":"string","title":"terminal"}}
        }
        """);

    void Because() => _actual = _schema.Properties["value"].ActualSchema;

    [Fact] void should_resolve_the_terminal_schema() => _actual.Title.ShouldEqual("terminal");
    [Fact] void should_leave_the_public_reference_one_hop() => _schema.Properties["value"].Reference!.HasReference.ShouldBeTrue();
    [Fact] void should_validate_against_the_terminal_type() => _schema.Validate("""{"value":42}""").Count.ShouldEqual(1);
    [Fact] void should_accept_the_terminal_type() => _schema.Validate("""{"value":"text"}""").Count.ShouldEqual(0);
    [Fact] void should_not_invent_chain_metadata() => _schema.HasComplianceMetadata().ShouldBeFalse();
}
