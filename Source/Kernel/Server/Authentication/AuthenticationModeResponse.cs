// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents how the Workbench's users sign in and out.
/// </summary>
/// <param name="Mode">How HTTP callers are authenticated: "chronicle" or "trustedProxy".</param>
/// <param name="SignOutUrl">Where to send a user who signs out, when a trusted proxy authenticates them; otherwise null.</param>
public record AuthenticationModeResponse(string Mode, string? SignOutUrl)
{
    /// <summary>
    /// The mode reported when Chronicle authenticates HTTP callers itself.
    /// </summary>
    public const string Chronicle = "chronicle";

    /// <summary>
    /// The mode reported when a trusted proxy authenticates HTTP callers.
    /// </summary>
    public const string TrustedProxy = "trustedProxy";

    /// <summary>
    /// Creates the response for the configured HTTP authentication.
    /// </summary>
    /// <param name="http">The HTTP authentication configuration.</param>
    /// <returns>The response.</returns>
    public static AuthenticationModeResponse From(HttpAuthentication http) => http.IsTrustedProxy
        ? new(TrustedProxy, string.IsNullOrWhiteSpace(http.TrustedProxy.SignOutUrl) ? null : http.TrustedProxy.SignOutUrl)
        : new(Chronicle, null);
}
