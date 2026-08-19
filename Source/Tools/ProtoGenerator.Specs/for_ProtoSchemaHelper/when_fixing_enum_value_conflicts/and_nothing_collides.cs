// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_fixing_enum_value_conflicts;

public class and_nothing_collides : Specification
{
    const string Schema = """
        syntax = "proto3";
        package Cratis.Chronicle.Contracts.EventSequences;
        enum EventSequenceQuerySortBy {
           SequenceNumber = 0;
           Occurred = 1;
        }
        message EventType {
           string Id = 1;
        }
        """;

    string _result;

    void Because() => _result = ProtoSchemaHelper.FixEnumValueConflicts(Schema);

    [Fact] void should_leave_the_schema_untouched() => _result.ShouldEqual(Schema);
}
