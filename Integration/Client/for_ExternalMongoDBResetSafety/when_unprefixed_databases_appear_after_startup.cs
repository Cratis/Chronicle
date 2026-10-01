// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_unprefixed_databases_appear_after_startup : Cratis.Specifications.Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ExternalMongoDBResetSafety().Verify("run_", ["billing"], ["billing", "run_chronicle+main", "run_System+es", "System+es"]));

    [Fact] void should_refuse_the_reset_even_when_the_prefixed_database_exists() => _error.ShouldBeOfExactType<ExternalMongoDBKernelPrefixNotVerified>();
    [Fact] void should_identify_the_unprefixed_database() => _error.Message.ShouldContain("System+es");
}
