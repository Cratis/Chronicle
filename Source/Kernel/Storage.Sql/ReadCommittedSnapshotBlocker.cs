// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// A session that holds a database open while read-committed snapshot is being enabled.
/// </summary>
/// <param name="SessionId">The session id.</param>
/// <param name="Status">The session status.</param>
/// <param name="ProgramName">The client program name.</param>
/// <param name="HostName">The client host name.</param>
/// <param name="RequestMode">The mode of the lock held on the database.</param>
/// <param name="RequestStatus">The status of that lock request.</param>
/// <param name="WaitType">The wait type of the session's running request, if any.</param>
/// <param name="BlockingSessionId">The session blocking this one, if any.</param>
internal sealed record ReadCommittedSnapshotBlocker(
    int SessionId,
    string? Status,
    string? ProgramName,
    string? HostName,
    string? RequestMode,
    string? RequestStatus,
    string? WaitType,
    int? BlockingSessionId);
