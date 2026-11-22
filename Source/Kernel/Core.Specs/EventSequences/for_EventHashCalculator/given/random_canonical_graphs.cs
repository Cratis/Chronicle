// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator.given;

public static class random_canonical_graphs
{
    static readonly string[] _text = [string.Empty, "z", "a", "é日本語", "🚀", "\uE000", "\u2028\u2029", "\0\b\f\n\r\t", "\"\\", "<>&+`"];

    public static IEnumerable<(string Name, ExpandoObject Content)> Generate()
    {
        var random = new Random(5821);
        var scalars = direct_value_corpus.Scalars().ToArray();
        var values = scalars.Select(scalar => scalar.Value).ToArray();
        for (var index = 0; index < scalars.Length * 8; index++)
        {
            // Cycle through every eligible scalar, rather than hoping random selection reaches each type.
            var scalar = scalars[index % scalars.Length];
            var content = extended_event_corpus.Content(("scalar", scalar.Value), ("graph", NextValue(random, 4, values)), ("text", NextText(random)));
            yield return ($"graph_{index}_{scalar.Name}", content);
        }
    }

    static object? NextValue(Random random, int depth, object?[] scalars)
    {
        if (depth == 0) return NextScalar(random, scalars);
        return random.Next(7) switch
        {
            0 => NextScalar(random, scalars),
            1 => extended_event_corpus.Content(Properties(random, depth - 1, scalars)),
            2 => Properties(random, depth - 1, scalars).ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal),
            3 => Items(random, depth - 1, scalars).ToList(),
            4 => Items(random, depth - 1, scalars).ToArray(),
            5 => Enumerable.Range(0, random.Next(5)).Select(_ => NextText(random)).ToArray(),
            _ => Enumerable.Range(0, random.Next(5)).Select(_ => extended_event_corpus.Content(Properties(random, depth - 1, scalars))).ToArray()
        };
    }

    static object? NextScalar(Random random, object?[] scalars) => random.Next(5) switch
    {
        0 => random.NextInt64(long.MinValue, long.MaxValue),
        1 => new decimal(random.Next(), random.Next(), random.Next(), random.Next(2) == 0, (byte)random.Next(29)),
        2 => Math.ScaleB((random.NextDouble() * 2) - 1, random.Next(-1074, 1024)),
        3 => NextText(random),
        _ => scalars[random.Next(scalars.Length)]
    };

    static IEnumerable<object?> Items(Random random, int depth, object?[] scalars) =>
        Enumerable.Range(0, random.Next(5)).Select(_ => NextValue(random, depth, scalars));

    static (string Name, object? Value)[] Properties(Random random, int depth, object?[] scalars) =>
        Enumerable.Range(0, random.Next(5)).Select(index => ($"{NextText(random)}{index}", NextValue(random, depth, scalars))).ToArray();

    static string NextText(Random random) => string.Concat(Enumerable.Range(0, random.Next(6)).Select(_ => _text[random.Next(_text.Length)]));
}
