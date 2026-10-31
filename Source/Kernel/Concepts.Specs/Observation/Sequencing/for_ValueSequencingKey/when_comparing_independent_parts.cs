// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Sequencing.for_ValueSequencingKey;

public class when_comparing_independent_parts : Specification
{
    ValueSequencingKey _first;
    ValueSequencingKey _second;
    bool _equal;

    void Establish()
    {
        _first = new(["a", "bc"]);
        _second = new(["a", "bc"]);
    }

    void Because() => _equal = _first == _second;

    [Fact] void should_compare_parts_by_ordinal_value() => _equal.ShouldBeTrue();
    [Fact] void should_have_equal_value_hash_codes() => _first.GetHashCode().ShouldEqual(_second.GetHashCode());
}
