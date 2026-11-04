// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_a_float_literal_is_not_representable : Specification
{
    readonly byte[] _key = Encoding.ASCII.GetBytes("slice-1-reference-key");
    string _literal;
    string _stored;

    void Because()
    {
        _literal = Value(float.Parse("1.00000001", System.Globalization.CultureInfo.InvariantCulture));
        _stored = Value(1.0f);
    }

    string Value(float number) => UniqueConstraintEncoder.Encode(_key, [new(number, "float")]).AsT0.Value;

    [Fact] void should_encode_like_the_stored_single() => _literal.ShouldEqual(_stored);
}
