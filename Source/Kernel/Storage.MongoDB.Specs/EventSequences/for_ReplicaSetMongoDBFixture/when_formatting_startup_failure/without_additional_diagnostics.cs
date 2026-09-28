// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_ReplicaSetMongoDBFixture.when_formatting_startup_failure;

public class without_additional_diagnostics : Specification
{
    Exception _startupError;
    Exception _result;

    void Establish() => _startupError = new TimeoutException("PRIMARY election timed out");
    void Because() => _result = ReplicaSetMongoDBFixture.WithStartupDiagnostics(_startupError, string.Empty, string.Empty);

    [Fact] void should_keep_the_original_error() => _result.ShouldEqual(_startupError);
}
