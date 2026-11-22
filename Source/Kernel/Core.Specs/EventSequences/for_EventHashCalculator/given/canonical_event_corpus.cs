// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator.given;

/// <summary>
/// The compatibility corpus shared by the byte and hash specifications.
/// </summary>
public static class canonical_event_corpus
{
    /// <summary>
    /// The type id also exercises UTF-8 prefix encoding.
    /// </summary>
    public const string EventType = "тип-🚀|<>&'\"\\\n";

    /// <summary>
    /// The source id includes an unmatched surrogate to pin replacement fallback.
    /// </summary>
    public const string EventSource = "源-é\uD800";

    /// <summary>
    /// Gets three successive content generations for each event shape.
    /// </summary>
    /// <returns>The content generations.</returns>
    public static IEnumerable<ExpandoObject[]> Generations() => Shapes().Select(content =>
    {
        var second = Object(("version", 2), ("previous", content));
        var third = Object(("version", 3), ("previous", second), ("added", Array.Empty<object>()));
        return new[] { content, second, third };
    });

    static IEnumerable<ExpandoObject> Shapes()
    {
        yield return Object(("z", "last"), ("a", 42), ("flag", false), ("missing", null));
        yield return Object(("z", Object(("b", 2), ("a", Object(("d", 4), ("c", 3))))), ("a", true));
        yield return Object(("mixed", new object?[] { null, true, false, "text", 3, Object(("z", 2), ("a", 1)), new[] { 3, 2, 1 } }), ("empty", Array.Empty<object>()));
        yield return Object();
        yield return Object(("decimal", 123.4500m), ("double", -3.141592653589793d), ("large", ulong.MaxValue), ("negative", long.MinValue), ("small", 1e-100d), ("exponent", 1e100d), ("zero", -0d));
        yield return Object(("text", "<>&'\"\\\b\f\n\r\t\0\u001f blå 日本語 🚀 \u2028\u2029"), ("surrogates", "\uD800|\uDC00"));
        yield return Object(("\uE000", "BMP"), ("🚀", "pair"), ("a\\\"\n", "escaped"), ("", "empty"), ("é", "accent"), ("a", "prefix"), ("a\0", "control"));
        yield return Object(("duplicates", JsonElement.Parse("{\"z\":0,\"a\":1,\"a\":2,\"\\u0061\":3,\"Z\":4}")));
    }

    static ExpandoObject Object(params (string Name, object? Value)[] properties)
    {
        var content = new ExpandoObject();
        var values = (IDictionary<string, object?>)content;
        foreach (var (name, value) in properties) values[name] = value;
        return content;
    }
}
