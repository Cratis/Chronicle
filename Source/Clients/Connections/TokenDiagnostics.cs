// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// The only place client token diagnostics are worded. Logs and exceptions carry fixed labels - never a host, port, path,
/// query, user info, remote text or exception message.
/// </summary>
internal static class TokenDiagnostics
{
    /// <summary>
    /// Gets the fixed label for a <see cref="TokenEndpointSource"/>.
    /// </summary>
    /// <param name="source">The <see cref="TokenEndpointSource"/>.</param>
    /// <returns>The label.</returns>
    internal static string Label(TokenEndpointSource source) => source switch
    {
        TokenEndpointSource.Configured => "configured",
        TokenEndpointSource.Authority => "authority",
        TokenEndpointSource.Discovered => "discovered",
        TokenEndpointSource.DefaultServer => "default server",
        _ => "unknown"
    };

    /// <summary>
    /// Gets the fixed label for a <see cref="TokenFailureCategory"/>.
    /// </summary>
    /// <param name="category">The <see cref="TokenFailureCategory"/>.</param>
    /// <returns>The label.</returns>
    internal static string Label(TokenFailureCategory category) => category switch
    {
        TokenFailureCategory.Unreachable => "unreachable",
        TokenFailureCategory.Timeout => "timeout",
        TokenFailureCategory.InvalidResponse => "invalid response",
        TokenFailureCategory.Rejected => "rejected",
        _ => "unknown"
    };

    /// <summary>
    /// Gets the fixed sentence used when a token request fails for a <see cref="TokenFailureCategory"/>.
    /// </summary>
    /// <param name="category">The <see cref="TokenFailureCategory"/>.</param>
    /// <returns>The sentence.</returns>
    internal static string Describe(TokenFailureCategory category) => "The token request failed (" + Label(category) + ").";

    /// <summary>
    /// Classifies a failure by its type and status only, never by its message.
    /// </summary>
    /// <param name="error">The failure to classify.</param>
    /// <returns>The <see cref="TokenFailureCategory"/>.</returns>
    internal static TokenFailureCategory Categorize(Exception error) => error switch
    {
        InsecureTokenEndpoint or JsonException or FormatException or InvalidOperationException or KeyNotFoundException => TokenFailureCategory.InvalidResponse,
        HttpRequestException { StatusCode: not null } => TokenFailureCategory.Rejected,
        TimeoutException or OperationCanceledException => TokenFailureCategory.Timeout,
        _ => TokenFailureCategory.Unreachable
    };
}
