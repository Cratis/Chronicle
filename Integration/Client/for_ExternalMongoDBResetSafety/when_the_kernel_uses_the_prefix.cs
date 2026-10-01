// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_the_kernel_uses_the_prefix : Cratis.Specifications.Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ExternalMongoDBResetSafety().Verify("run_", ["billing", "chronicle+main", "System+es"], ["billing", "chronicle+main", "System+es", "run_chronicle+main", "run_System+es"]));

    [Fact] void should_allow_the_reset_without_rejecting_preexisting_unprefixed_databases() => _error.ShouldBeNull();
}
