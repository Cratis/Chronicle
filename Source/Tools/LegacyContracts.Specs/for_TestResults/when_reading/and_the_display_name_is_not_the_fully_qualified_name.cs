// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_TestResults.when_reading;

public class and_the_display_name_is_not_the_fully_qualified_name : Specification
{
    IReadOnlyList<TestResult> _results;

    void Because() => _results = TestResults.Parse(EvaluationFixture.PassingTrx.Replace("testId=", "testName=\"unrelated display name\" testId=", StringComparison.Ordinal));

    [Fact] void should_use_the_definition_to_build_the_fully_qualified_name() => _results.Single().Name.ShouldEqual(EvaluationFixture.TestName);
    [Fact] void should_read_the_passing_result() => _results.Single().Passed.ShouldBeTrue();
}
