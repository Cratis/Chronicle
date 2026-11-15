// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_framing_crosses_a_byte_boundary : Specification
{
    byte[] _tuple;
    byte[] _scalar;
    byte[] _array;
    byte[] _unicode;
    byte[] _emptyArray;

    void Because()
    {
        _tuple = UniqueConstraintEncoder.GetPreimage(Enumerable.Repeat(new UniqueConstraintComponent(Value: null), 256).ToArray());
        _scalar = UniqueConstraintEncoder.GetPreimage([new(new string('x', 256))]);
        _array = UniqueConstraintEncoder.GetPreimage([new(Enumerable.Repeat("x", 256).ToArray())]);
        _unicode = UniqueConstraintEncoder.GetPreimage([new("😀")]);
        _emptyArray = UniqueConstraintEncoder.GetPreimage([new(Array.Empty<string>())]);
    }

    [Fact] void should_write_a_big_endian_tuple_count() => Convert.ToHexString(_tuple.AsSpan(20, 4)).ShouldEqual("00000100");
    [Fact] void should_keep_each_null_position() => _tuple.Length.ShouldEqual(20 + 4 + 256);
    [Fact] void should_write_the_scalar_tag() => _scalar[24].ShouldEqual((byte)0x01);
    [Fact] void should_write_a_big_endian_scalar_byte_length() => Convert.ToHexString(_scalar.AsSpan(25, 4)).ShouldEqual("00000100");
    [Fact] void should_write_the_array_tag() => _array[24].ShouldEqual((byte)0x02);
    [Fact] void should_write_a_big_endian_array_count() => Convert.ToHexString(_array.AsSpan(25, 4)).ShouldEqual("00000100");
    [Fact] void should_measure_unicode_as_utf8_bytes() => Convert.ToHexString(_unicode.AsSpan(25, 4)).ShouldEqual("00000004");
    [Fact] void should_write_utf8_without_a_bom() => Convert.ToHexString(_unicode.AsSpan(29)).ShouldEqual("F09F9880");
    [Fact] void should_frame_an_empty_array_as_present() => Convert.ToHexString(_emptyArray.AsSpan(24)).ShouldEqual("0200000000");
}
