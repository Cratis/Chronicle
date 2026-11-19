// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents how Chronicle reads the user a trusted reverse proxy has already authenticated.
/// </summary>
/// <remarks>
/// The headers are accepted only on HTTP requests - never on gRPC calls - whose immediate peer is a trusted proxy:
/// loopback, or an address in <see cref="ForwardedHeaders.KnownProxies"/> or <see cref="ForwardedHeaders.KnownNetworks"/>.
/// The proxy must remove any of these headers a caller sends before adding its own.
/// </remarks>
public class TrustedProxyAuthentication
{
    /// <summary>The default issuer given to users authenticated by the proxy.</summary>
    public const string DefaultIssuer = "chronicle:proxy";

    /// <summary>
    /// Gets the header carrying the authenticated user's stable identifier. Required. Defaults to "X-Forwarded-User".
    /// </summary>
    public string UserHeader { get; init; } = "X-Forwarded-User";

    /// <summary>
    /// Gets the header carrying the authenticated user's email address, if any. Defaults to "X-Forwarded-Email".
    /// </summary>
    public string? EmailHeader { get; init; } = "X-Forwarded-Email";

    /// <summary>
    /// Gets the header carrying the authenticated user's display name, if any. Defaults to "X-Forwarded-Preferred-Username".
    /// </summary>
    public string? NameHeader { get; init; } = "X-Forwarded-Preferred-Username";

    /// <summary>
    /// Gets the header carrying the authenticated user's comma-separated groups, if any. Defaults to "X-Forwarded-Groups".
    /// </summary>
    public string? GroupsHeader { get; init; } = "X-Forwarded-Groups";

    /// <summary>
    /// Gets the group that makes a proxy-authenticated user a Chronicle administrator.
    /// When not set, every user the proxy authenticates is an administrator.
    /// </summary>
    public string? AdministratorGroup { get; init; }

    /// <summary>
    /// Gets the issuer given to proxy-authenticated users. It identifies them in audit records and access decisions.
    /// Defaults to <see cref="DefaultIssuer"/>.
    /// </summary>
    public string Issuer { get; init; } = DefaultIssuer;

    /// <summary>
    /// Gets an optional secret the proxy must send in <see cref="SharedSecretHeader"/>, in addition to coming from a trusted address.
    /// Store it as a secret.
    /// </summary>
    public string? SharedSecret { get; init; }

    /// <summary>
    /// Gets the header carrying <see cref="SharedSecret"/>. Defaults to "X-Chronicle-Proxy-Secret".
    /// </summary>
    public string SharedSecretHeader { get; init; } = "X-Chronicle-Proxy-Secret";

    /// <summary>
    /// Gets the address the Workbench sends the user to when they sign out, for example the proxy's sign-out endpoint.
    /// Either an absolute HTTP(S) URL or a path starting with "/". When not set, the Workbench offers no sign-out.
    /// </summary>
    public string? SignOutUrl { get; init; }
}
