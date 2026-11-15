// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Represents the authentication configuration.
/// </summary>
/// <remarks>
/// Applies to client credentials only. The token is requested from, in order of precedence:
/// <see cref="TokenEndpoint"/>, the token endpoint discovered from <see cref="Authority"/>, or the Chronicle server itself.
/// External endpoints must be absolute https URLs without user info, and their certificates are always fully validated.
/// </remarks>
public class Authentication
{
    /// <summary>
    /// Gets or sets the authentication authority URL. If not configured, uses the internal OAuth authority.
    /// </summary>
    /// <remarks>
    /// When set, the client discovers the token endpoint from <c language="csharp">{Authority}/.well-known/openid-configuration</c>.
    /// Must be an absolute https URL. Ignored when <see cref="TokenEndpoint"/> is set.
    /// </remarks>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the token endpoint to request client credentials tokens from.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="Authority"/>. Used as is, without discovery.
    /// Must be an absolute https URL. If not configured, the client requests tokens from the Chronicle server.
    /// </remarks>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the scope to request with the client credentials token.
    /// </summary>
    /// <remarks>
    /// Only sent when <see cref="TokenEndpoint"/> or <see cref="Authority"/> is configured.
    /// </remarks>
    public string? Scope { get; set; }
}
