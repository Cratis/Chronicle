// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_TestResults.when_reading;

public class and_one_project_ran_no_tests : Specification
{
    string _directory;
    IReadOnlyList<TestResult> _results;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"legacy-contracts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "populated.trx"), EvaluationFixture.PassingTrx);
        File.WriteAllText(Path.Combine(_directory, "filtered-out.trx"), "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\" />");
    }

    void Because() => _results = TestResults.Read(_directory);

    [Fact] void should_read_the_results_of_the_populated_project() => _results.Single().Name.ShouldEqual(EvaluationFixture.TestName);

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
