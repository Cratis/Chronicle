// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_formatting_startup_failure;

public class and_the_kernel_is_incompatible_without_additional_diagnostics : Specification
{
    MongoDBKernelIncompatible _startupError;
    Exception _result;

    void Establish() => _startupError = new(new InvalidOperationException("known incompatibility with this version of MongoDB"));
    void Because() => _result = ReplicaSetMongoDBFixture.WithStartupDiagnostics(_startupError, null, null);

    [Fact] void should_keep_the_kernel_diagnostic_exception() => _result.ShouldEqual(_startupError);
}
