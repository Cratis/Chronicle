// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_CompositeKeyEncoding.when_encoding;

public class and_decimals_differ_only_in_trailing_zeros : Specification
{
    string _short;
    string _long;
    string _whole;

    void Because()
    {
        _short = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = 1.5m });
        _long = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = 1.50m });
        _whole = CompositeKeyEncoding.Encode(new Dictionary<string, object?> { ["a"] = 2.000m });
    }

    [Fact] void should_give_the_same_value() => _long.ShouldEqual(_short);
    [Fact] void should_drop_the_trailing_zeros() => _short.ShouldEqual("~k1:3:1.5");
    [Fact] void should_write_a_whole_decimal_without_a_fraction() => _whole.ShouldEqual("~k1:1:2");
}
