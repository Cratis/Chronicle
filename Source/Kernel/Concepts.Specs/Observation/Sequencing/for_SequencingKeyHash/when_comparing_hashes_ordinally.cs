// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_SequencingKeyHash;

public class when_comparing_hashes_ordinally : Specification
{
    SequencingKeyHash _lower;
    SequencingKeyHash _higher;
    int _comparison;

    void Establish()
    {
        var lower = new byte[32];
        var higher = new byte[32];
        lower[31] = 0x7f;
        higher[31] = 0x80;
        _lower = SequencingKeyHash.FromStored(lower);
        _higher = SequencingKeyHash.FromStored(higher);
    }

    void Because() => _comparison = _lower.CompareTo(_higher);

    [Fact] void should_compare_unsigned_bytes() => _comparison.ShouldBeLessThan(0);
    [Fact] void should_use_the_same_operator_order() => (_lower < _higher).ShouldBeTrue();
    [Fact] void should_order_null_before_a_hash() => _lower.CompareTo(null).ShouldBeGreaterThan(0);
}
