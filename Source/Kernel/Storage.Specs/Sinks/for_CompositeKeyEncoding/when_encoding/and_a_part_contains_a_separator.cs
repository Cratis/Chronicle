// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_a_part_contains_a_separator : Specification
{
    string[] _results;

    void Because()
    {
        (string A, string B)[] keys =
        [
            ("a_b", "c"),
            ("a", "b_c"),
            ("a~b", "c"),
            ("a", "~bc"),
            ("a:1", "c"),
            ("a", "1:c"),
            ("1:a", "b"),
            ("1", "a:b"),
            ("-", "c"),
            ("", "-c"),
            ("~k1:", "x"),
            ("", "")
        ];
        _results = [.. keys.Select(key => CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = key.A, ["b"] = key.B }))];
    }

    [Fact] void should_give_every_key_its_own_value() => _results.Distinct().Count().ShouldEqual(_results.Length);
    [Fact] void should_keep_the_underscore_inside_the_value() => _results[0].ShouldEqual("~k1:3:a_b1:c");
    [Fact] void should_keep_the_colon_inside_the_value() => _results[4].ShouldEqual("~k1:3:a:11:c");
}
