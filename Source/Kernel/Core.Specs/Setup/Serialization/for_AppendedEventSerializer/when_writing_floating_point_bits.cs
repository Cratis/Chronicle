// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_writing_floating_point_bits : given.a_compact_serializer
{
    object[] _values;
    object[] _result;
    byte[] _bytes;

    void Establish() => _values = [-0f, float.Epsilon, float.MaxValue, BitConverter.Int32BitsToSingle(0x7fc00001), -0d, double.Epsilon, double.MaxValue, BitConverter.Int64BitsToDouble(0x7ff8000000000001), double.PositiveInfinity];

    void Because()
    {
        var input = appended_event with { Content = ContentWith(("values", _values)) };
        _bytes = serializer.SerializeToArray(input);
        var decoded = serializer.Deserialize(_bytes)!;
        _result = (object[])((IDictionary<string, object?>)decoded.Content)["values"]!;
    }

    [Fact] void should_use_compact() => WireTypeOf(_bytes).ShouldEqual(WireType.TagDelimited);
    [Fact] void should_preserve_each_numeric_type() => _result.Select(item => item.GetType()).ShouldEqual(_values.Select(item => item.GetType()));
    [Fact] void should_preserve_every_bit() => _result.Select(Bits).ShouldEqual(_values.Select(Bits));

    static long Bits(object value) => value is float single ? BitConverter.SingleToInt32Bits(single) : BitConverter.DoubleToInt64Bits((double)value);
}
