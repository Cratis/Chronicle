// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_the_value_is_an_array : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _array;
    string _reordered;
    string _scalar;
    string _empty;
    string _nullComponent;

    void Because()
    {
        _array = Value(new[] { "a", "b" });
        _reordered = Value(new[] { "b", "a" });
        _scalar = Value("a");
        _empty = Value(Array.Empty<string>());
        _nullComponent = Value(null);
    }

    string Value(object? value) => UniqueConstraintEncoder.Encode(_key, [new(value), new("tail")]).AsT0.Value;

    [Fact] void should_preserve_element_order() => _array.ShouldNotEqual(_reordered);
    [Fact] void should_distinguish_an_array_from_a_scalar() => _array.ShouldNotEqual(_scalar);
    [Fact] void should_distinguish_an_empty_array_from_null() => _empty.ShouldNotEqual(_nullComponent);
}
