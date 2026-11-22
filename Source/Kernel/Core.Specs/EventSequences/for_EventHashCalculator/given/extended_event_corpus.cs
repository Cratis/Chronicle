// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator.given;

public static class extended_event_corpus
{
    public static IEnumerable<ExpandoObject> Shapes()
    {
        yield return Content(("integers", new object[] { byte.MaxValue, sbyte.MinValue, short.MinValue, ushort.MaxValue, int.MinValue, uint.MaxValue, long.MinValue, ulong.MaxValue, Int128.MinValue, UInt128.MaxValue }));
        yield return Content(("numbers", new object[] { float.Epsilon, float.MaxValue, -0f, double.Epsilon, double.MaxValue, -0d, (Half)1.5, decimal.MinValue, decimal.MaxValue, 0.0000m, 1.2300m }));
        yield return Content(("big", BigInteger.Parse("123456789012345678901234567890")), ("negative", new BigInteger(-42)));
        yield return Content(("utc", new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234567)), ("unspecified", new DateTime(2025, 1, 2, 3, 4, 5)), ("offset", new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.FromHours(5.5)).AddTicks(1234567)));
        yield return Content(("guid", Guid.Parse("01234567-89ab-cdef-0123-456789abcdef")), ("span", TimeSpan.FromTicks(-1234567890123)), ("date", new DateOnly(2025, 1, 2)), ("time", new TimeOnly(3, 4, 5).Add(TimeSpan.FromTicks(1234567))));
        yield return Content(("enum", DayOfWeek.Friday), ("named", named_value.Second), ("concept", new EventSourceId("source<>&")), ("type", new EventTypeId("type")));
        yield return Content(("dictionary", new Dictionary<string, object?> { ["z"] = null, ["a"] = new List<object?> { true, "<>&", Content(("b", 2), ("a", 1)) } }), ("bytes", new byte[] { 0, 1, 254, 255 }), ("typed", new[] { 3, 2, 1 }));
        yield return Content(("node", JsonNode.Parse("{\"z\":[null,true,1.2300,1e+10],\"a\":\"<>&\\uD83D\\uDE80\"}")), ("element", JsonElement.Parse("{\"b\":-0.00,\"a\":1e-09,\"a\":2}")));
        yield return Content(("\uD800", 1), ("\uDC00", 2), ("\uFFFD", 3), ("text", "\uD800|\uDC00|🚀"), ("char", '\uD800'));
        yield return Content(("object", new payload("last", 42)), ("characters", new[] { '<', '"', '\n', 'é' }));
    }

    /// <summary>
    /// Keeps Shapes and its literal pins intact, but exercises each property without fallback siblings.
    /// </summary>
    /// <returns>Independent events with explicitly expected direct/fallback selection.</returns>
    public static IEnumerable<(string Name, ExpandoObject Content, bool Direct)> SplitShapes()
    {
        string[] directProperties = ["integers", "numbers", "utc", "unspecified", "offset", "guid", "span", "date", "time", "dictionary", "bytes", "\uFFFD"];
        foreach (var shape in Shapes())
        {
            foreach (var property in (IDictionary<string, object?>)shape)
            {
                var direct = directProperties.Contains(property.Key, StringComparer.Ordinal);
                yield return (property.Key, Content((property.Key, property.Value)), direct);
            }
        }
    }

    public static ExpandoObject Content(params (string Name, object? Value)[] properties)
    {
        var content = new ExpandoObject();
        var values = (IDictionary<string, object?>)content;
        foreach (var (name, value) in properties) values[name] = value;
        return content;
    }

    [JsonConverter(typeof(JsonStringEnumConverter<named_value>))]
    public enum named_value
    {
        First = 0,
        Second = 1
    }

    public record payload(string Z, int A);
}
