// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Reports the first confirmed exit of the kernel container required by an integration fixture.
/// Subsequent resets retain this failure instead of waiting for another connection timeout.
/// Diagnostics contain only fixed crash markers; arbitrary log content is redacted.
/// </summary>
public sealed partial class KernelProcessExited : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KernelProcessExited"/> class.
    /// </summary>
    /// <param name="containerId">The exited container's identity.</param>
    /// <param name="exitCode">The confirmed process exit code.</param>
    /// <param name="tail">The bounded diagnostic output.</param>
    internal KernelProcessExited(string containerId, long exitCode, string tail)
        : base($"Kernel container '{containerId}' exited with code {exitCode}: {Sanitize(tail)}")
    {
    }

    // A denylist cannot recognize an arbitrary multiline environment value or credential.
    // Only these fixed markers may leave the fixture; no captured substrings are included.
    [GeneratedRegex(@"Out of memory\.|Stack overflow\.|Fatal error\.|System\.(?:OutOfMemoryException|InvalidOperationException|IOException)", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex CrashMarker { get; }

    static string Sanitize(string tail)
    {
        if (tail == "[diagnostics unavailable]")
        {
            return tail;
        }
        var bounded = tail;
        if (tail.Length > 16384)
        {
            bounded = tail[^16384..];
            var endOfPartialLine = bounded.IndexOf('\n', StringComparison.Ordinal);
            bounded = endOfPartialLine < 0 ? string.Empty : bounded[(endOfPartialLine + 1)..];
        }
        return string.Join('\n', bounded.Split('\n').TakeLast(64).Select(line =>
        {
            var marker = CrashMarker.Match(line);
            return marker.Success ? marker.Value : "[redacted]";
        }));
    }
}
