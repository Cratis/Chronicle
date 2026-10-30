// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_a_part_is_null_and_another_empty : Specification
{
    string _null;
    string _empty;
    string _missing;

    void Because()
    {
        _null = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = null, ["b"] = "x" });
        _empty = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = string.Empty, ["b"] = "x" });
        _missing = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = null, ["b"] = null });
    }

    [Fact] void should_write_null_as_a_dash() => _null.ShouldEqual("~k1:-1:x");
    [Fact] void should_write_empty_with_a_zero_length() => _empty.ShouldEqual("~k1:0:1:x");
    [Fact] void should_distinguish_null_from_empty() => _null.ShouldNotEqual(_empty);
    [Fact] void should_write_every_missing_part_as_a_dash() => _missing.ShouldEqual("~k1:--");
}
