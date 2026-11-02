// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_AppliedEventMarkers;

public class when_hashing_the_same_value_in_two_namespaces : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = AppliedEventMarkers.StreamHash("one", "source");
        _second = AppliedEventMarkers.StreamHash("two", "source");
    }

    [Fact] void should_differ() => _first.ShouldNotEqual(_second);
}
