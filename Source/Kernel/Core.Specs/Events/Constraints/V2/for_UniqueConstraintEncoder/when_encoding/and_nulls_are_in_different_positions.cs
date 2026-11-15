// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_nulls_are_in_different_positions : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _trailingNull;
    string _leadingNull;
    string _single;

    void Because()
    {
        _trailingNull = Value([new("x"), new(null)]);
        _leadingNull = Value([new(null), new("x")]);
        _single = Value([new("x")]);
    }

    string Value(UniqueConstraintComponent[] components) => UniqueConstraintEncoder.Encode(_key, components).AsT0.Value;

    [Fact] void should_distinguish_a_trailing_null_from_a_leading_null() => _trailingNull.ShouldNotEqual(_leadingNull);
    [Fact] void should_distinguish_a_trailing_null_from_a_shorter_tuple() => _trailingNull.ShouldNotEqual(_single);
    [Fact] void should_distinguish_a_leading_null_from_a_shorter_tuple() => _leadingNull.ShouldNotEqual(_single);
}
