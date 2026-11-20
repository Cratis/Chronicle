// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents how Chronicle reads the user a trusted reverse proxy has already authenticated.
/// </summary>
/// <remarks>
/// The headers are accepted only on HTTP requests - never on gRPC calls - and only from a proxy Chronicle can recognize,
/// by its address, by a shared secret, or by both:
/// <list type="bullet">
/// <item><description>With address trust configured (<see cref="KnownProxies"/>, <see cref="KnownNetworks"/> or <see cref="TrustLoopback"/>),
/// the immediate peer must be one of those addresses.</description></item>
/// <item><description>With <see cref="SharedSecret"/> configured, the request must carry it in <see cref="SharedSecretHeader"/>.</description></item>
/// <item><description>With both, both must hold. With neither, startup fails.</description></item>
/// </list>
/// The address list is deliberately separate from <see cref="ForwardedHeaders"/>: a peer trusted to report a client address
/// is not thereby trusted to say who the user is. The proxy must remove any of these headers a caller sends before adding its own.
/// </remarks>
public class TrustedProxyAuthentication
{
    /// <summary>The default issuer given to users authenticated by the proxy.</summary>
    public const string DefaultIssuer = "chronicle:proxy";

    /// <summary>
    /// Gets the IP addresses of the proxies whose identity headers are trusted.
    /// </summary>
    public IEnumerable<string> KnownProxies { get; init; } = [];

    /// <summary>
    /// Gets the CIDR ranges of the proxies whose identity headers are trusted. Every peer in a range can claim to be any user,
    /// so keep them narrow and consider <see cref="SharedSecret"/>.
    /// </summary>
    public IEnumerable<string> KnownNetworks { get; init; } = [];

    /// <summary>
    /// Gets whether a proxy connecting over loopback - a sidecar in the same pod or container - is trusted. Defaults to false,
    /// because every process on the host, and every port-forward, connects over loopback too.
    /// </summary>
    public bool TrustLoopback { get; init; }

    /// <summary>
    /// Gets whether the proxy is recognized by its address.
    /// </summary>
    public bool HasAddressTrust => TrustLoopback || KnownProxies.Any() || KnownNetworks.Any();

    /// <summary>
    /// Gets whether the proxy is recognized by a shared secret.
    /// </summary>
    public bool HasSharedSecret => !string.IsNullOrEmpty(SharedSecret);

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
    /// Gets a secret the proxy must send in <see cref="SharedSecretHeader"/>. On its own it recognizes the proxy wherever it
    /// connects from - the natural choice where addresses change, such as in Kubernetes. Combined with address trust, both must hold.
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
