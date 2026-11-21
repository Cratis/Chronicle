// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_resolving_a_reference;

public class to_a_ref_chain_cycle : Specification
{
    JsonSchema _schema;
    JsonSchema _actual;

    void Establish() => _schema = JsonSchema.FromJson("""
        {"$ref":"#/$defs/A","$defs":{"A":{"$ref":"#/$defs/B","title":"A"},"B":{"$ref":"#/$defs/A","title":"B"}}}
        """);

    void Because() => _actual = _schema.ActualSchema;

    [Fact] void should_stop_at_the_last_resolved_schema() => _actual.Title.ShouldEqual("B");
    [Fact] void should_not_report_metadata_in_a_cycle_without_it() => _schema.HasComplianceMetadata().ShouldBeFalse();
}
