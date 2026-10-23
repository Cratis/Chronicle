// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_TestResults.when_reading;

public class and_no_project_ran_any_tests : Specification
{
    string _directory;
    Exception _result;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"legacy-contracts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "first.trx"), "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\" />");
        File.WriteAllText(Path.Combine(_directory, "second.trx"), "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\" />");
    }

    void Because() => _result = Catch.Exception(() => TestResults.Read(_directory));

    [Fact] void should_refuse_to_evaluate() => _result.ShouldBeOfExactType<CannotEvaluate>();

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
