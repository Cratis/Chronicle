// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_formatting_startup_failure;

public class and_logs_cannot_be_retrieved_or_the_container_disposed : Specification
{
    Exception _startupError;
    Exception _result;

    void Establish() => _startupError = new TimeoutException("PRIMARY election timed out");
    void Because() => _result = ReplicaSetMongoDBFixture.WithStartupDiagnostics(
        _startupError, null, null, "Docker log timeout", "Docker dispose refused");

    [Fact] void should_include_the_original_error_message() => _result.Message.ShouldContain("PRIMARY election timed out");
    [Fact] void should_report_the_log_retrieval_error() => _result.Message.ShouldContain("MongoDB fixture log retrieval error: Docker log timeout");
    [Fact] void should_report_the_disposal_error() => _result.Message.ShouldContain("MongoDB fixture disposal error: Docker dispose refused");
    [Fact] void should_preserve_the_original_error() => _result.InnerException.ShouldEqual(_startupError);
}
