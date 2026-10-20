// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Converts membership rows without losing votes or silo identity.
/// </summary>
internal static class MembershipEntryConverters
{
    /// <summary>
    /// Converts an Orleans entry to a SQL row.
    /// </summary>
    /// <param name="entry">The membership entry.</param>
    /// <param name="clusterId">The owning cluster.</param>
    /// <param name="etag">The row concurrency token.</param>
    /// <returns>The persisted row.</returns>
    internal static MembershipRow ToSql(this MembershipEntry entry, string clusterId, int etag) => new(
        clusterId,
        entry.SiloAddress.Endpoint.Address.ToString(),
        entry.SiloAddress.Endpoint.Port,
        entry.SiloAddress.Generation,
        entry.SiloName,
        entry.HostName,
        (int)entry.Status,
        entry.ProxyPort,
        entry.SuspectTimes is null ? null : JsonSerializer.Serialize(entry.SuspectTimes.Select(vote => new Suspect(vote.Item1.ToParsableString(), vote.Item2.ToUniversalTime().Ticks))),
        Milliseconds(entry.StartTime),
        Milliseconds(entry.IAmAliveTime),
        etag,
        entry.RoleName,
        entry.UpdateZone,
        entry.FaultZone);

    /// <summary>
    /// Restores an Orleans entry from SQL.
    /// </summary>
    /// <param name="row">The persisted row.</param>
    /// <returns>The restored entry.</returns>
    internal static MembershipEntry ToOrleans(this MembershipRow row) => new()
    {
        SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Parse(row.Address), row.Port), row.Generation),
        SiloName = row.SiloName,
        HostName = row.HostName,
        Status = (SiloStatus)row.Status,
        ProxyPort = row.ProxyPort,
        SuspectTimes = row.SuspectTimes is null ? null : JsonSerializer.Deserialize<Suspect[]>(row.SuspectTimes)!
            .Select(vote => Tuple.Create(SiloAddress.FromParsableString(vote.Silo), new DateTime(vote.UtcTicks, DateTimeKind.Utc))).ToList(),
        StartTime = DateTime.SpecifyKind(row.StartTime, DateTimeKind.Utc),
        IAmAliveTime = DateTime.SpecifyKind(row.IAmAliveTime, DateTimeKind.Utc),
        RoleName = row.RoleName,
        UpdateZone = row.UpdateZone,
        FaultZone = row.FaultZone
    };

    /// <summary>
    /// Normalizes UTC timestamps to the storage precision.
    /// </summary>
    /// <param name="time">The timestamp.</param>
    /// <returns>The UTC timestamp at millisecond precision.</returns>
    internal static DateTime Milliseconds(DateTime time)
    {
        var utc = time.ToUniversalTime();
        return new DateTime(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }

    sealed record Suspect(string Silo, long UtcTicks);
}
