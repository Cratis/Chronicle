// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Recognizes the dynamic dictionary suffixes understood by the projection mappers.
/// </summary>
internal static partial class DynamicMappingTarget
{
    [GeneratedRegex("^(?<container>[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*)\\.(?<source>\\$eventContent|\\$eventContext|\\$causedBy)\\.(?<key>[A-Za-z_][A-Za-z0-9_]*(?:\\.[A-Za-z_][A-Za-z0-9_]*)*)$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TargetPattern { get; }

    /// <summary>
    /// Reads a supported dynamic dictionary target.
    /// </summary>
    /// <param name="target">The target path.</param>
    /// <param name="container">The dictionary container path.</param>
    /// <param name="source">The dynamic key source.</param>
    /// <param name="key">The key property path.</param>
    /// <returns>Whether the target has a supported dynamic suffix.</returns>
    internal static bool TryRead(string target, out string container, out string source, out string key)
    {
        var match = TargetPattern.Match(target);
        container = match.Groups["container"].Value;
        source = match.Groups["source"].Value;
        key = match.Groups["key"].Value;
        return match.Success;
    }

    /// <summary>
    /// Validates a dynamic dictionary target and its context or identity key.
    /// </summary>
    /// <param name="target">The target path.</param>
    /// <returns>The resolution error, or null for a supported target.</returns>
    internal static string? GetError(string target)
    {
        if (!TryRead(target, out _, out var source, out var key))
        {
            return "Dictionary mappings require a container followed by an event-content, event-context or causing-identity key";
        }

        return source switch
        {
            "$eventContext" => EventContextPaths.GetResolutionError(key),
            "$causedBy" when key is not ("subject" or "name" or "userName") => "The causing-identity dictionary key must be subject, name or userName",
            _ => null
        };
    }
}
