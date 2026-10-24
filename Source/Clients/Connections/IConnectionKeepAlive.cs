// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines a connection that can report whether it skips the keep-alive stream.
/// </summary>
/// <remarks>
/// Connections that do not implement this contract are treated as keeping the connection alive.
/// </remarks>
internal interface IConnectionKeepAlive
{
    /// <summary>
    /// Gets whether the keep-alive stream and its watchdog are skipped for this connection.
    /// </summary>
    bool IsSkipped { get; }
}
