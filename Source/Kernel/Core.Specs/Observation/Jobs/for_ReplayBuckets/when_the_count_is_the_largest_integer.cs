// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBuckets;

public class when_the_count_is_the_largest_integer : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ReplayBuckets.For("order-42", int.MaxValue));

    [Fact] void should_reject_the_count() => _exception.ShouldBeOfExactType<ReplayBucketCountOutOfRange>();
}
