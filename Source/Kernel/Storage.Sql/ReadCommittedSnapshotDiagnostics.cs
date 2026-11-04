// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// What is known about why enabling read-committed snapshot on a database is waiting.
/// </summary>
/// <param name="State">The database state.</param>
/// <param name="UserAccess">The database user access mode.</param>
/// <param name="ReadCommittedSnapshotOn">Whether read-committed snapshot is on.</param>
/// <param name="WaiterWaitType">The wait type of the session enabling it.</param>
/// <param name="WaiterBlockingSessionId">The session the enabling session waits for.</param>
/// <param name="Blockers">The other sessions holding the database.</param>
/// <param name="Unavailable">Why the diagnostics could not be read, if they could not.</param>
internal sealed record ReadCommittedSnapshotDiagnostics(
    string? State,
    string? UserAccess,
    bool? ReadCommittedSnapshotOn,
    string? WaiterWaitType,
    int? WaiterBlockingSessionId,
    IReadOnlyList<ReadCommittedSnapshotBlocker> Blockers,
    string? Unavailable = null)
{
    /// <summary>
    /// Describes the diagnostics in one bounded line. It never contains logins, connection strings or statement text.
    /// </summary>
    /// <returns>The description.</returns>
    internal string Describe()
    {
        if (Unavailable is not null)
        {
            return $"diagnostics unavailable ({SafeReason(Unavailable)})";
        }

        var parts = new List<string>
        {
            $"database state {SafeValue(State)}, user access {SafeValue(UserAccess)}, read-committed snapshot {Describe(ReadCommittedSnapshotOn)}",
            $"enabling session wait {SafeValue(WaiterWaitType)}" + (WaiterBlockingSessionId is { } waiter ? $" blocked by session {waiter.ToString(CultureInfo.InvariantCulture)}" : string.Empty)
        };

        if (Blockers.Count == 0)
        {
            parts.Add("no other sessions visible (VIEW SERVER STATE may be missing)");
        }
        else
        {
            parts.Add("sessions holding the database: " + string.Join("; ", Blockers.Take(10).Select(Describe)));
        }

        return string.Join(", ", parts);
    }

    static string Describe(bool? on) => on switch
    {
        true => "on",
        false => "off",
        _ => "unknown"
    };

    static string Describe(ReadCommittedSnapshotBlocker blocker) =>
        $"session {blocker.SessionId.ToString(CultureInfo.InvariantCulture)} (wait {SafeValue(blocker.WaitType)}, blocking session {blocker.BlockingSessionId?.ToString(CultureInfo.InvariantCulture) ?? "none"})";

    static string SafeValue(string? value) =>
        value is { Length: > 0 and <= 60 } && value.All(_ => _ is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_')
            ? value
            : "unknown";

    static string SafeReason(string reason) => reason switch
    {
        "timeout" or "diagnostic failure" => reason,
        _ when reason.StartsWith("SQL error ", StringComparison.Ordinal) &&
            int.TryParse(reason.AsSpan(10), NumberStyles.None, CultureInfo.InvariantCulture, out var number) => $"SQL error {number.ToString(CultureInfo.InvariantCulture)}",
        _ => "diagnostic failure"
    };
}
