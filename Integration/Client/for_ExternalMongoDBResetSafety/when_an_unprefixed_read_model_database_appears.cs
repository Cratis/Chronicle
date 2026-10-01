// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_an_unprefixed_read_model_database_appears : Cratis.Specifications.Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ExternalMongoDBResetSafety.Verify("run_", [], ["run_chronicle+main", "run_Testing", "Testing"]));

    [Fact] void should_refuse_the_reset() => _error.ShouldBeOfExactType<ExternalMongoDBKernelPrefixNotVerified>();
}
