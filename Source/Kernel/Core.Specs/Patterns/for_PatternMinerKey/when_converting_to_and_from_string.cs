// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns.for_PatternMinerKey;

public class when_converting_to_and_from_string : Specification
{
    static readonly PatternMinerKey _key = new("some-store", "some-namespace");

    string _asString;
    PatternMinerKey _parsed;

    void Because()
    {
        _asString = _key;
        _parsed = PatternMinerKey.Parse(_asString);
    }

    [Fact] void should_hold_the_event_store_after_the_round_trip() => _parsed.EventStore.ShouldEqual(_key.EventStore);
    [Fact] void should_hold_the_namespace_after_the_round_trip() => _parsed.Namespace.ShouldEqual(_key.Namespace);
}
