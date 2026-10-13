// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Captures.Engine.DeclarationLanguage;

/// <summary>
/// Carries legacy events-source settings alongside the source accepted by Screenplay.
/// </summary>
/// <param name="Source">The source to parse, retaining the original line numbers.</param>
/// <param name="Sequence">The first legacy sequence setting, if present.</param>
/// <param name="Names">The original event names keyed by their parser-safe placeholders.</param>
internal partial record LegacyEventsSource(string Source, string? Sequence, IReadOnlyDictionary<string, string> Names)
{
    [GeneratedRegex(@"^(?<setting>sequence|from)(?:\s+(?<value>.*))?$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Setting { get; }

    [GeneratedRegex(@"^[A-Za-z_][\w.]*$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EventName { get; }

    /// <summary>
    /// Lifts sequence settings and event identifiers that Screenplay cannot parse out of an events source.
    /// </summary>
    /// <param name="source">The original declaration.</param>
    /// <returns>The parser-safe source and the settings to restore after visiting.</returns>
    public static LegacyEventsSource Lift(string source)
    {
        var lines = source.Split('\n');
        var names = new Dictionary<string, string>();
        string? sequence = null;
        int? sourceIndent = null;
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var content = line.Trim();
            if (content.Length == 0 || content.StartsWith("//", StringComparison.Ordinal) || content.StartsWith('#'))
            {
                continue;
            }

            var indent = line.Length - line.TrimStart().Length;
            if (sourceIndent is not null && indent <= sourceIndent)
            {
                sourceIndent = null;
            }

            if (content == "source events")
            {
                sourceIndent = indent;
                continue;
            }

            if (sourceIndent is null)
            {
                continue;
            }

            var setting = Setting.Match(content);
            if (!setting.Success)
            {
                continue;
            }

            var value = setting.Groups["value"].Value.Trim();
            if (setting.Groups["setting"].Value == "sequence")
            {
                sequence ??= value;
                lines[index] = line.EndsWith('\r') ? "\r" : string.Empty;
            }
            else if (value.Length > 0 && !EventName.IsMatch(value))
            {
                var placeholder = names.FirstOrDefault(pair => pair.Value == value).Key;
                if (placeholder is null)
                {
                    var suffix = names.Count;
                    do
                    {
                        placeholder = $"__chronicleEvent{suffix++}";
                    }
                    while (source.Contains(placeholder, StringComparison.Ordinal) || names.ContainsKey(placeholder));
                    names.Add(placeholder, value);
                }

                lines[index] = $"{line[..indent]}from {placeholder}{(line.EndsWith('\r') ? "\r" : string.Empty)}";
            }
        }

        return new(string.Join('\n', lines), sequence, names);
    }
}
