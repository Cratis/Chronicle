// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Holds the address of the peer that opened the connection, captured before forwarded headers rewrite it.
/// </summary>
/// <remarks>
/// Forwarded-headers processing replaces <see cref="ConnectionInfo.RemoteIpAddress"/> with the client address a proxy
/// reports. Deciding whether to trust a proxy's identity headers needs the proxy's own address, which only this keeps.
/// </remarks>
public interface IImmediatePeerFeature
{
    /// <summary>
    /// Gets the address of the immediate peer, if the transport reported one.
    /// </summary>
    IPAddress? Address { get; }
}
