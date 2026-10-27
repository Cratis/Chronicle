// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_PublicApiBaseline.given;

internal static class public_api_difference
{
    internal static void RequireMatch(string? expected, string current)
    {
        var actualLines = Lines(current);
        Validate(actualLines);
        var expectedLines = Lines(expected ?? string.Empty);
        var removed = expectedLines.Except(actualLines, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var added = actualLines.Except(expectedLines, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var malformed = expectedLines.Length == 0 || !IsCanonical(expectedLines) || expected != string.Join('\n', expectedLines) + "\n";
        if (!expectedLines.SequenceEqual(actualLines, StringComparer.Ordinal) || malformed)
        {
            const int Limit = 20;
            var summary = removed.Take(Limit).Select(line => $"- {line}").Concat(added.Take(Limit).Select(line => $"+ {line}"));
            throw new InvalidOperationException($"Contracts public API differs (or baseline is missing, empty or malformed).\nRemoved: {removed.Length}; added: {added.Length}. Showing at most {Limit} lines of each.\n{string.Join('\n', summary)}\nReview for_PublicApiBaseline/README.md before deliberate regeneration.");
        }
    }

    static string[] Lines(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    static bool IsCanonical(string[] lines) => lines.SequenceEqual(lines.Order(StringComparer.Ordinal), StringComparer.Ordinal)
        && lines.Distinct(StringComparer.Ordinal).Count() == lines.Length
        && lines.All(line => !string.IsNullOrWhiteSpace(line) && !line.Contains('\r'));

    static void Validate(string[] lines)
    {
        if (lines.Length == 0 || !IsCanonical(lines))
        {
            throw new InvalidOperationException("Current public API must be nonempty, unique and ordinally sorted with LF endings.");
        }
    }
}
