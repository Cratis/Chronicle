// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_reading_invalid_compact_scalars : given.a_compact_serializer
{
    Exception?[] _errors;

    static byte[] Value(byte tag, params long[] numbers) =>
        [1, 1, 0, 0, 0, 1, 0, 0, 0, 97, tag, .. numbers.SelectMany(BitConverter.GetBytes)];

    void Because()
    {
        byte[][] contents = [
            [.. Value(17, 0), 9],
            [.. Value(17, -1), 0],
            Value(18, 0, long.MaxValue),
            Value(19, 4_000_000),
            Value(20, -1),
            Value(21, 0).Take(10).ToArray(),
            Value(15, 0, 0, 0, 0xFF0000),
            Value(15, long.MaxValue, 0, 0, 0),
            Value(9, long.MaxValue)];
        _errors = contents.Select(content => Record.Exception(() => serializer.Deserialize(CompactMessage(1, content)))).ToArray();
    }

    [Fact] void should_reject_an_unknown_date_time_kind() => _errors[0].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_out_of_range_date_time_ticks() => _errors[1].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_an_out_of_range_offset() => _errors[2].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_an_out_of_range_date() => _errors[3].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_out_of_range_time_of_day() => _errors[4].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_a_truncated_scalar() => _errors[5].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_an_invalid_decimal_scale() => _errors[6].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_decimal_parts_outside_the_int_range() => _errors[7].ShouldBeOfExactType<InvalidDataException>();
    [Fact] void should_reject_integers_outside_the_type_range() => _errors[8].ShouldBeOfExactType<InvalidDataException>();
}
