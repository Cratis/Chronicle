// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_AppliedEventMarkers;

public class when_hashing_two_values_whose_concatenation_is_the_same : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = AppliedEventMarkers.StreamHash("ab", "seq", "c");
        _second = AppliedEventMarkers.StreamHash("a", "seq", "bc");
    }

    [Fact] void should_differ() => _first.ShouldNotEqual(_second);
}
