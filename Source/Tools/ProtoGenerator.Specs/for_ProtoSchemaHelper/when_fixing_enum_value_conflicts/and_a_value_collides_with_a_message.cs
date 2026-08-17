// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_fixing_enum_value_conflicts;

public class and_a_value_collides_with_a_message : Specification
{
    const string Schema = """
        syntax = "proto3";

        message EventType {
           string id = 1;
        }

        enum EventSequenceQuerySortBy {
           Occurred = 0;
           EventType = 1;
        }
        """;

    string _result;

    void Because() => _result = ProtoSchemaHelper.FixEnumValueConflicts(Schema);

    [Fact] void should_prefix_the_colliding_value() => _result.ShouldContain("EVENT_SEQUENCE_QUERY_SORT_BY_EventType = 1;");
    [Fact] void should_leave_the_non_colliding_value_alone() => _result.ShouldContain("Occurred = 0;");
    [Fact] void should_leave_the_message_declaration_alone() => _result.ShouldContain("message EventType {");
}
