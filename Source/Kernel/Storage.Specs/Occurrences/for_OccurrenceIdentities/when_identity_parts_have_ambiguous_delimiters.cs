// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences.for_OccurrenceIdentities;

public class when_identity_parts_have_ambiguous_delimiters : Specification
{
    OccurrenceId _first;
    OccurrenceId _second;

    void Because()
    {
        _first = OccurrenceIdentities.ForIngress("a:b", "c");
        _second = OccurrenceIdentities.ForIngress("a", "b:c");
    }

    [Fact] void should_distinguish_the_part_boundaries() => _first.ShouldNotEqual(_second);
    [Fact] void should_produce_a_sha256_identity() => _first.Value.Length.ShouldEqual(64);
}
