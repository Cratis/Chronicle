// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_the_parts_are_in_another_order : Specification
{
    string _first;
    string _second;

    void Establish() { }

    void Because()
    {
        _first = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["Invoice"] = "b", ["Currency"] = "a" });
        _second = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["Currency"] = "a", ["Invoice"] = "b" });
    }

    [Fact] void should_give_the_same_value() => _second.ShouldEqual(_first);
    [Fact] void should_order_the_parts_by_name() => _first.ShouldEqual("~k1:1:a1:b");
}
