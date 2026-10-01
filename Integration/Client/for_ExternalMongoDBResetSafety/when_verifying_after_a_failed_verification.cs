// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_verifying_after_a_failed_verification : Cratis.Specifications.Specification
{
    ExternalMongoDBResetSafety _safety;
    Exception _error;

    void Establish()
    {
        _safety = new();
        Catch.Exception(() => _safety.Verify("run_", [], ["chronicle+main"]));
    }

    void Because() => _error = Catch.Exception(() => _safety.Verify("run_", [], ["chronicle+main"]));

    [Fact] void should_still_refuse_the_reset() => _error.ShouldBeOfExactType<ExternalMongoDBKernelPrefixNotVerified>();
    [Fact] void should_not_be_verified() => _safety.IsVerified.ShouldBeFalse();
}
