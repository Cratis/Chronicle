// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_ComplianceJsonSchemaExtensions.when_checking_for_compliance_metadata;

public class and_ordinary_steps_exceed_the_state_budget : Specification
{
    JsonSchema _schema;
    bool _result;

    void Establish()
    {
        var properties = new JsonObject();
        for (var index = 0; index < 4200; index++)
        {
            properties[$"value{index}"] = new JsonObject { ["type"] = "string" };
        }

        _schema = new JsonSchema(new JsonObject { ["type"] = "object", ["properties"] = properties });
    }

    void Because() => _result = _schema.HasComplianceMetadata();

    [Fact] void should_fail_closed_for_an_unexplored_ordinary_graph() => _result.ShouldBeTrue();
    [Fact] void should_mark_the_exhausted_walk_as_indeterminate() => _schema.MetadataGraph.Indeterminate.ShouldBeTrue();
    [Fact] void should_not_invent_handler_metadata() => _schema.GetComplianceMetadata().ShouldBeEmpty();
}
