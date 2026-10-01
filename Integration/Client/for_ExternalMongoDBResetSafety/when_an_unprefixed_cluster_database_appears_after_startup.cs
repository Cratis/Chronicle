// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_an_unprefixed_cluster_database_appears_after_startup : Cratis.Specifications.Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ExternalMongoDBResetSafety().Verify("run_", [], ["run_chronicle+main", "chronicle+main"]));

    [Fact] void should_refuse_the_reset() => _error.ShouldBeOfExactType<ExternalMongoDBKernelPrefixNotVerified>();
    [Fact] void should_identify_the_unprefixed_database() => _error.Message.ShouldContain("chronicle+main");
}
