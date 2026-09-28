// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_formatting_startup_failure;

public class and_the_kernel_is_incompatible_with_logs : Specification
{
    MongoDBKernelIncompatible _startupError;
    Exception _result;

    void Establish() => _startupError = new(new InvalidOperationException("known incompatibility with this version of MongoDB"));
    void Because() => _result = ReplicaSetMongoDBFixture.WithStartupDiagnostics(_startupError, "mongod refused to start", string.Empty);

    [Fact] void should_keep_the_kernel_guidance_visible() => _result.Message.ShouldContain(_startupError.Message);
    [Fact] void should_include_the_mongod_log() => _result.Message.ShouldContain("mongod refused to start");
    [Fact] void should_preserve_the_kernel_exception() => _result.InnerException.ShouldEqual(_startupError);
}
