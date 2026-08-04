// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_declaring_reserved_fields.stand_ins;

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_declaring_reserved_fields;

public class and_a_type_has_retired_field_numbers : Specification
{
    const string Schema = """
        syntax = "proto3";

        message SomethingElse {
           string name = 1;
        }

        message TypeWithRetiredFields {
           string name = 2;
        }
        """;

    string _result;

    void Because() => _result = ProtoSchemaHelper.DeclareReservedFields(Schema, [typeof(TypeWithRetiredFields)]);

    [Fact] void should_reserve_them_in_ascending_order() => _result.ShouldContain("reserved 1, 3, 7;");
    [Fact] void should_reserve_them_inside_the_message_that_retired_them() =>
        _result.IndexOf("reserved 1, 3, 7;", StringComparison.Ordinal).ShouldBeGreaterThan(_result.IndexOf("message TypeWithRetiredFields {", StringComparison.Ordinal));

    [Fact] void should_leave_a_message_that_retired_nothing_alone() =>
        _result[.._result.IndexOf("message TypeWithRetiredFields {", StringComparison.Ordinal)].ShouldNotContain("reserved");

    [Fact] void should_keep_the_declared_fields() => _result.ShouldContain("string name = 2;");
}
