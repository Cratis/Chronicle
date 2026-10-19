// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Stores the complete membership record and its concurrency token.
/// </summary>
/// <param name="ClusterId">The cluster id.</param>
/// <param name="Address">The address.</param>
/// <param name="Port">The port.</param>
/// <param name="Generation">The generation.</param>
/// <param name="SiloName">The silo name.</param>
/// <param name="HostName">The host name.</param>
/// <param name="Status">The status.</param>
/// <param name="ProxyPort">The proxy port.</param>
/// <param name="SuspectTimes">The suspect times.</param>
/// <param name="StartTime">The start time.</param>
/// <param name="IAmAliveTime">The i am alive time.</param>
/// <param name="ETag">The e tag.</param>
/// <param name="RoleName">The role name.</param>
/// <param name="UpdateZone">The update zone.</param>
/// <param name="FaultZone">The fault zone.</param>
internal record MembershipRow(
    string ClusterId,
    string Address,
    int Port,
    int Generation,
    string SiloName,
    string HostName,
    int Status,
    int ProxyPort,
    string? SuspectTimes,
    DateTime StartTime,
    DateTime IAmAliveTime,
    int ETag,
    string? RoleName,
    int UpdateZone,
    int FaultZone);
