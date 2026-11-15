// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Grpc.Core;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Turns exceptions into text that is safe to put in a status, a log or a metric: no key material, pins, paths or server supplied detail.
/// </summary>
public static partial class FederationErrorScrubber
{
    const int MaxLength = 200;
    const string Redacted = "[redacted]";

    [GeneratedRegex(@"sha256:[A-Za-z0-9+/=_-]+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex PinPattern { get; }

    [GeneratedRegex(@"(?:[A-Za-z]:\\|/)[^\s'""]*[/\\][^\s'""]*", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex PathPattern { get; }

    [GeneratedRegex(@"[A-Za-z0-9+/_=-]{24,}", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TokenPattern { get; }

    /// <summary>
    /// Scrub an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="secrets">Known secret strings, such as configured certificate paths, to remove wherever they occur.</param>
    /// <returns>The scrubbed description.</returns>
    public static string Scrub(Exception exception, IEnumerable<string?>? secrets = null)
    {
        // Detail supplied by a remote peer is untrusted text; the status code says all an operator needs.
        if (exception is RpcException rpc)
        {
            return $"gRPC {rpc.StatusCode}";
        }

        var innermost = exception;
        while (innermost.InnerException is not null)
        {
            innermost = innermost.InnerException;
        }

        var name = innermost == exception ? exception.GetType().Name : $"{exception.GetType().Name} ({innermost.GetType().Name})";
        return $"{name}: {ScrubText(innermost.Message, secrets)}";
    }

    /// <summary>
    /// Scrub arbitrary text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="secrets">Known secret strings to remove.</param>
    /// <returns>The scrubbed, length limited text.</returns>
    public static string ScrubText(string text, IEnumerable<string?>? secrets = null)
    {
        var result = text;
        foreach (var secret in secrets ?? [])
        {
            if (!string.IsNullOrEmpty(secret))
            {
                result = result.Replace(secret, Redacted, StringComparison.Ordinal);
            }
        }

        result = PinPattern.Replace(result, Redacted);
        result = PathPattern.Replace(result, Redacted);
        result = TokenPattern.Replace(result, Redacted);
        result = result.ReplaceLineEndings(" ");
        return result.Length > MaxLength ? result[..MaxLength] : result;
    }
}
