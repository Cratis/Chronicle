// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_formatting_startup_failure;

public class and_rs_initiate_fails : Specification
{
    Exception _startupError;
    Exception _result;

    void Establish() => _startupError = new InvalidOperationException("container exited");
    void Because() => _result = ReplicaSetMongoDBFixture.WithStartupDiagnostics(
        _startupError, "mongod output", "MongoDB fixture rs.initiate failed (mongosh exit 23)");

    [Fact] void should_identify_initiation_failure() => _result.Message.ShouldContain("MongoDB fixture rs.initiate failed: container exited");
    [Fact] void should_include_mongosh_stderr() => _result.Message.ShouldContain("MongoDB fixture rs.initiate failed (mongosh exit 23)");
    [Fact] void should_include_mongod_stdout() => _result.Message.ShouldContain("mongod output");
    [Fact] void should_preserve_the_original_error() => _result.InnerException.ShouldEqual(_startupError);
}
