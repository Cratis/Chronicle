// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator.given;

public static class direct_value_corpus
{
    public static IEnumerable<(string Name, object? Value)> Scalars()
    {
        yield return ("null", null);
        yield return ("true", true);
        yield return ("false", false);
        yield return ("byte", byte.MaxValue);
        yield return ("sbyte", sbyte.MinValue);
        yield return ("short", short.MinValue);
        yield return ("ushort", ushort.MaxValue);
        yield return ("int", int.MinValue);
        yield return ("uint", uint.MaxValue);
        yield return ("long", long.MinValue);
        yield return ("ulong", ulong.MaxValue);
        yield return ("int128", Int128.MinValue);
        yield return ("uint128", UInt128.MaxValue);
        yield return ("decimal_min", decimal.MinValue);
        yield return ("decimal_max", decimal.MaxValue);
        yield return ("decimal_scale", 1.2300m);
        yield return ("decimal_zero", 0.0000m);
        yield return ("decimal_negative_zero", new decimal(0, 0, 0, true, 4));
        yield return ("double_min", double.MinValue);
        yield return ("double_max", double.MaxValue);
        yield return ("double_epsilon", double.Epsilon);
        yield return ("double_negative_zero", -0d);
        yield return ("float_min", float.MinValue);
        yield return ("float_max", float.MaxValue);
        yield return ("float_epsilon", float.Epsilon);
        yield return ("float_negative_zero", -0f);
        yield return ("half_min", Half.MinValue);
        yield return ("half_max", Half.MaxValue);
        yield return ("half_epsilon", Half.Epsilon);
        yield return ("half_negative_zero", (Half)(-0f));
        yield return ("empty_string", string.Empty);
        yield return ("escaped_string", "\u2028\u2029+`");
        yield return ("unicode_string", "é日本語🚀\uE000\uFFFD");
        yield return ("control_string", new string(Enumerable.Range(0, 32).Select(index => (char)index).ToArray()));
        yield return ("quotes_and_slashes", "<>&'\"\\/");
        yield return ("utc", new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567));
        yield return ("local", new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Local).AddTicks(1234567));
        yield return ("unspecified", new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Unspecified).AddTicks(1234567));
        yield return ("offset", new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.FromHours(5.5)).AddTicks(1234567));
        yield return ("guid", Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"));
        yield return ("time_span", TimeSpan.FromTicks(-1234567890123));
        yield return ("date_only", new DateOnly(2025, 1, 2));
        yield return ("time_only", new TimeOnly(3, 4, 5).Add(TimeSpan.FromTicks(1234567)));
        yield return ("bytes", new byte[] { 0, 1, 254, 255 });
        yield return ("empty_bytes", Array.Empty<byte>());
        foreach (var character in "<>&\"'\\\0\n\té\u2028\u2029+`") yield return ($"char_{(int)character}", character);
    }

    public static IEnumerable<(string Name, ExpandoObject Content, bool Direct)> Shapes()
    {
        foreach (var (name, value) in Scalars()) yield return ($"scalar_{name}", extended_event_corpus.Content(("value", value)), true);
        yield return ("base64_and_escaped_text", extended_event_corpus.Content(("bytes", new byte[] { 0, 1, 254, 255 }), ("text", "\u2028\u2029+`")), true);
        yield return ("list", extended_event_corpus.Content(("list", new List<object?> { null, '<', "\u2028🚀", 1.2300m })), true);
        yield return ("object_array", extended_event_corpus.Content(("array", new object?[] { null, '\n', true, -0d })), true);
        yield return ("string_array", extended_event_corpus.Content(("array", new[] { "\u2028\u2029", "🚀\uE000", "\0\n\"\\" })), true);
        yield return ("expando_array", extended_event_corpus.Content(("array", new[] { extended_event_corpus.Content(("z", 1), ("a", 2)) })), true);
        yield return ("ordinal_names", extended_event_corpus.Content(("\uE000", 1), ("🚀", 2), ("\u2028\u2029", 3), ("\0\n\"\\", 4)), true);
    }
}
