// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.CodeGeneration;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Defines the grain that owns the dialer-initiated session of one subject. The key is the subject, so any silo can accept the
/// connection and the cluster still has exactly one current session for the subject.
/// </summary>
/// <remarks>
/// The connection lives on the silo that accepted it; the grain decides which connection is current. A newer connection supersedes the
/// older one, which finds out the next time it renews and ends, so a dialer that reconnects through another silo is never served twice.
/// </remarks>
[Version(1)]
public interface IFederationSession : IGrainWithStringKey
{
    /// <summary>
    /// Make a connection the current session of the subject, superseding any other.
    /// </summary>
    /// <param name="connectionId">The id of the accepted connection.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task Open(string connectionId);

    /// <summary>
    /// Confirm that a connection is still the current session. When the grain holds no session, for instance after it was deactivated,
    /// the connection becomes the current one, so a healthy connection is never ended by a grain that merely forgot.
    /// </summary>
    /// <param name="connectionId">The id of the connection.</param>
    /// <returns>True when the connection is the current session, false when a newer connection superseded it.</returns>
    [AlwaysInterleave]
    Task<bool> Renew(string connectionId);

    /// <summary>
    /// End a session. Ending a connection that is not the current one changes nothing.
    /// </summary>
    /// <param name="connectionId">The id of the connection.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task Close(string connectionId);
}
