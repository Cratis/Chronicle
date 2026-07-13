// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the HTTP protocols a server endpoint serves.
/// </summary>
public enum EndpointProtocols
{
    /// <summary>
    /// HTTP/1.1 only — the Workbench, REST API, OAuth and health surface.
    /// </summary>
    Http1 = 0,

    /// <summary>
    /// HTTP/2 only — cleartext gRPC (h2c) with prior knowledge.
    /// </summary>
    Http2 = 1,

    /// <summary>
    /// HTTP/1.1 and HTTP/2 multiplexed on one port, negotiated per connection through ALPN (requires TLS).
    /// </summary>
    Http1AndHttp2 = 2
}
