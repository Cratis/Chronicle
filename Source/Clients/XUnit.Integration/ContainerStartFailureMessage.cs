// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Formats bounded evidence for a terminal kernel container startup failure.
/// </summary>
internal static partial class ContainerStartFailureMessage
{
    const int MaxTailCharacters = 4096;

    // Keep the pattern identical to Integration/Client/MongoContainerStartupDiagnostics.cs, which is not shipped in this package.
    [GeneratedRegex(@"password|passwd|pwd|credential|secret|token|authorization|authentication|api.?key|private.?key|mongodb(?:\+srv)?://|https?://", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SensitiveLine { get; }

    /// <summary>
    /// Describes the failed startup with its exit code and most recent log lines.
    /// </summary>
    /// <param name="image">The kernel image.</param>
    /// <param name="attempts">The number of startup attempts.</param>
    /// <param name="exitCode">The exit code, if it could be read.</param>
    /// <param name="logTail">The available container logs.</param>
    /// <param name="maxLines">The maximum number of log lines to retain.</param>
    /// <returns>One bounded startup failure message.</returns>
    internal static string Describe(string image, int attempts, long? exitCode, string logTail, int maxLines)
    {
        var exit = exitCode.HasValue ? $"exit code {exitCode.Value.ToString(CultureInfo.InvariantCulture)}" : "exit code unavailable";
        var lines = logTail.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n')
            .Where(line => !SensitiveLine.IsMatch(line))
            .TakeLast(maxLines);
        var tail = string.Join(Environment.NewLine, lines);
        if (tail.Length > MaxTailCharacters) tail = tail[^MaxTailCharacters..];
        return $"Failed to start container '{image}' after {attempts} attempts ({exit}).{Environment.NewLine}{tail}";
    }
}
