// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines where a token endpoint came from. The only endpoint information that diagnostics may carry.
/// </summary>
internal enum TokenEndpointSource
{
    /// <summary>
    /// The endpoint is the configured Authentication:TokenEndpoint.
    /// </summary>
    Configured = 0,

    /// <summary>
    /// The endpoint is the configured Authentication:Authority.
    /// </summary>
    Authority = 1,

    /// <summary>
    /// The endpoint was discovered from the configured authority.
    /// </summary>
    Discovered = 2,

    /// <summary>
    /// The endpoint is the default token endpoint of the Chronicle server.
    /// </summary>
    DefaultServer = 3
}
