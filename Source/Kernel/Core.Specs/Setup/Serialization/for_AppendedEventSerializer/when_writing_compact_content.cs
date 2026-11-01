// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer;

public class when_writing_compact_content : given.a_compact_serializer
{
    AppendedEvent _input;
    AppendedEvent _result;
    byte[] _bytes;

    void Establish() => _input = appended_event with
    {
        Content = ContentWith(
            ("text", "An order — 😀\0"),
            ("nil", null),
            ("empty", ContentWith()),
            ("nested", new object?[] { null, ContentWith(("Name", "upper"), ("name", "lower")), new object[] { 1, 2L } }),
            ("sbyte", sbyte.MinValue),
            ("byte", byte.MaxValue),
            ("short", short.MinValue),
            ("ushort", ushort.MaxValue),
            ("int", int.MinValue),
            ("uint", uint.MaxValue),
            ("long", long.MinValue),
            ("ulong", ulong.MaxValue),
            ("float", 1.5f),
            ("double", double.Epsilon),
            ("decimal", 1.10m),
            ("guid", Guid.Parse("9bc1cc52-659b-40fa-845b-976ad96380f2")),
            ("utc", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)),
            ("local", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Local)),
            ("unspecified", new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified)),
            ("offset", new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(330))),
            ("date", new DateOnly(2026, 1, 2)),
            ("time", new TimeOnly(3, 4, 5).Add(TimeSpan.FromTicks(1234567))),
            ("span", TimeSpan.FromTicks(-123456789)),
            ("bytes", new byte[] { 0, 1, 255 }),
            ("bool", true))
    };

    void Because()
    {
        _bytes = serializer.SerializeToArray(_input);
        _result = serializer.Deserialize(_bytes)!;
    }

    [Fact] void should_use_a_tag_delimited_field() => WireTypeOf(_bytes).ShouldEqual(WireType.TagDelimited);
    [Fact] void should_preserve_values_types_keys_and_order() => AssertEquivalent(_result, _input);
    [Fact] void should_preserve_decimal_scale() => decimal.GetBits((decimal)((IDictionary<string, object?>)_result.Content)["decimal"]!).ShouldEqual(decimal.GetBits(1.10m));
    [Fact] void should_preserve_datetime_kinds() => new[] { "utc", "local", "unspecified" }.Select(key => ((DateTime)((IDictionary<string, object?>)_result.Content)[key]!).Kind).ShouldEqual([DateTimeKind.Utc, DateTimeKind.Local, DateTimeKind.Unspecified]);
}
