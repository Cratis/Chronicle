// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_AppliedEventMarkers;

public class when_hashing_the_same_stream_twice : Specification
{
    string _first;
    string _second;

    void Because()
    {
        _first = AppliedEventMarkers.StreamHash("ns", "source");
        _second = AppliedEventMarkers.StreamHash("ns", "source");
    }

    [Fact] void should_be_the_same() => _first.ShouldEqual(_second);
    [Fact] void should_be_22_url_safe_characters() => System.Text.RegularExpressions.Regex.IsMatch(_first, "^[A-Za-z0-9_-]{22}$").ShouldBeTrue();
}
