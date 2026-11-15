// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Extracts the loggable part of a failed token response: the standard OAuth error code and nothing else.
/// </summary>
/// <remarks>
/// Token failure logs do not include response bodies. Only the error codes defined by RFC 6749 are surfaced.
/// </remarks>
static class OAuthErrorCodes
{
    const string NoErrorCode = "no standard OAuth error code in the response";

    static readonly HashSet<string> _allowed = new(StringComparer.Ordinal)
    {
        "invalid_request",
        "invalid_client",
        "invalid_grant",
        "unauthorized_client",
        "unsupported_grant_type",
        "invalid_scope",
        "access_denied",
        "unsupported_response_type",
        "server_error",
        "temporarily_unavailable"
    };

    /// <summary>
    /// Describes a failed token response body for logging.
    /// </summary>
    /// <param name="responseBody">The response body.</param>
    /// <returns>The allowlisted error code, or a fixed text when there is none. Never any part of the body.</returns>
    internal static string Describe(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String &&
                error.GetString() is { } code &&
                _allowed.Contains(code))
            {
                return $"error={code}";
            }
        }
        catch (JsonException)
        {
            // Not JSON - nothing to surface.
        }

        return NoErrorCode;
    }
}
