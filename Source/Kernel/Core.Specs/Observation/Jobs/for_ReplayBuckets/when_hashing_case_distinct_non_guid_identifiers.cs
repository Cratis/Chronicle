// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayBuckets;

public class when_hashing_case_distinct_non_guid_identifiers : Specification
{
    ReplayBucket _lowerCase;
    ReplayBucket _upperCase;

    void Because()
    {
        _lowerCase = ReplayBuckets.For("order-42", 64);
        _upperCase = ReplayBuckets.For("Order-42", 64);
    }

    [Fact] void should_hash_lower_case_ordinally() => _lowerCase.Index.ShouldEqual(20);
    [Fact] void should_hash_upper_case_ordinally() => _upperCase.Index.ShouldEqual(52);
}
