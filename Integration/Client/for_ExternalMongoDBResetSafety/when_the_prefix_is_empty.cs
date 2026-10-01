// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_ExternalMongoDBResetSafety;

public class when_the_prefix_is_empty : Cratis.Specifications.Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new ExternalMongoDBResetSafety().Verify(string.Empty, ["chronicle+main"], ["chronicle+main"]));

    [Fact] void should_refuse_the_reset() => _error.ShouldBeOfExactType<ExternalMongoDBKernelPrefixNotVerified>();
}
