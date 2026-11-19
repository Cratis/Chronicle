// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents how browser and HTTP API callers - the Workbench and its API - are authenticated.
/// </summary>
/// <remarks>
/// This only separates how HTTP callers prove who they are. gRPC clients keep authenticating with their
/// client credentials whatever the mode, and none of it applies when <see cref="Authentication.Enabled"/> is off.
/// </remarks>
public class HttpAuthentication
{
    /// <summary>
    /// Gets how HTTP callers are authenticated. Defaults to <see cref="HttpAuthenticationMode.Chronicle"/>.
    /// </summary>
    public HttpAuthenticationMode Mode { get; init; } = HttpAuthenticationMode.Chronicle;

    /// <summary>
    /// Gets the settings used when <see cref="Mode"/> is <see cref="HttpAuthenticationMode.TrustedProxy"/>.
    /// </summary>
    public TrustedProxyAuthentication TrustedProxy { get; init; } = new();

    /// <summary>
    /// Gets whether HTTP callers are authenticated by a trusted reverse proxy.
    /// </summary>
    public bool IsTrustedProxy => Mode == HttpAuthenticationMode.TrustedProxy;
}
