// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_fixing_enum_value_conflicts;

/// <summary>
/// An enum value shares its scope with everything the package declares, so being named after a message beside it
/// is the same collision as being named after another enum's value.
/// </summary>
/// <remarks>
/// protobuf-net's parser accepts this and protoc does not, so it costs nothing at generation time and fails the
/// next build that reads the schema in another language - which is how EventSequenceQuerySortBy.EventType reached
/// a committed eventsequences.proto that the TypeScript client could not compile.
/// </remarks>
public class and_a_value_is_named_after_a_message : Specification
{
    const string Schema = """
        syntax = "proto3";
        package Cratis.Chronicle.Contracts.EventSequences;
        enum EventSequenceQuerySortBy {
           SequenceNumber = 0;
           Occurred = 1;
           EventType = 2;
        }
        message EventType {
           string Id = 1;
        }
        """;

    string _result;

    void Because() => _result = ProtoSchemaHelper.FixEnumValueConflicts(Schema);

    [Fact] void should_prefix_the_colliding_value() => _result.ShouldContain("EVENT_SEQUENCE_QUERY_SORT_BY_EventType = 2;");
    [Fact] void should_leave_the_message_alone() => _result.ShouldContain("message EventType {");
    [Fact] void should_leave_the_values_that_collide_with_nothing() => _result.ShouldContain("SequenceNumber = 0;");
}
