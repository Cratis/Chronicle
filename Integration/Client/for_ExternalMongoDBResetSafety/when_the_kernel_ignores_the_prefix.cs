// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_the_kernel_ignores_the_prefix : Cratis.Specifications.Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ExternalMongoDBResetSafety().Verify("run_", ["billing"], ["billing", "chronicle+main", "System+es"]));

    [Fact] void should_refuse_the_reset() => _error.ShouldBeOfExactType<ExternalMongoDBKernelPrefixNotVerified>();
    [Fact] void should_explain_the_data_loss_risk() => _error.Message.ShouldContain("delete unrelated databases");
}
