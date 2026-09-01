// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.ProtoGenerator.for_ProtoSchemaHelper.when_fixing_enum_value_conflicts;

public class and_values_collide_across_enums : Specification
{
    const string Schema = """
        syntax = "proto3";

        enum JobStatus {
           None = 0;
           Running = 1;
        }

        enum StepStatus {
           None = 0;
           Done = 1;
        }
        """;

    string _result;

    void Because() => _result = ProtoSchemaHelper.FixEnumValueConflicts(Schema);

    [Fact] void should_prefix_the_value_in_the_first_enum() => _result.ShouldContain("JOB_STATUS_None = 0;");
    [Fact] void should_prefix_the_value_in_the_second_enum() => _result.ShouldContain("STEP_STATUS_None = 0;");
    [Fact] void should_leave_unique_values_alone() =>
        (_result.Contains("Running = 1;") && _result.Contains("Done = 1;")).ShouldBeTrue();
}
