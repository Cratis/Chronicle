// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBuckets;

public class when_the_count_exceeds_the_limit : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ReplayBuckets.For("order-42", 65));

    [Fact] void should_reject_the_count() => _exception.ShouldBeOfExactType<ReplayBucketCountOutOfRange>();
}
