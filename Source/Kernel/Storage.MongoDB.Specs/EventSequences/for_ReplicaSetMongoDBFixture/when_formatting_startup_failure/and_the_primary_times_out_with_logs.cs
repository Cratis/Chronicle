// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_formatting_startup_failure;

public class and_the_primary_times_out_with_logs : Specification
{
    TimeoutException _startupError;
    Exception _result;

    void Establish() => _startupError = new("MongoDB PRIMARY readiness timed out");
    void Because() => _result = ReplicaSetMongoDBFixture.WithStartupDiagnostics(
        _startupError, $"obsolete stdout{new string('o', 4096)}stdout tail", $"obsolete stderr{new string('e', 4096)}stderr tail");

    [Fact] void should_report_a_startup_failure() => _result.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_include_the_original_error_message() => _result.Message.ShouldContain("MongoDB PRIMARY readiness timed out");
    [Fact] void should_include_the_stdout_tail() => _result.Message.ShouldContain("MongoDB fixture stdout:\n");
    [Fact] void should_include_the_stdout_content() => _result.Message.ShouldContain("stdout tail");
    [Fact] void should_include_the_stderr_tail() => _result.Message.ShouldContain("MongoDB fixture stderr:\n");
    [Fact] void should_include_the_stderr_content() => _result.Message.ShouldContain("stderr tail");
    [Fact] void should_bound_stdout() => _result.Message.ShouldNotContain("obsolete stdout");
    [Fact] void should_bound_stderr() => _result.Message.ShouldNotContain("obsolete stderr");
    [Fact] void should_preserve_the_original_error() => _result.InnerException.ShouldEqual(_startupError);
}
