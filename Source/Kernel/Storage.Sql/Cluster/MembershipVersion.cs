// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Stores the cluster-wide membership version.
/// </summary>
/// <param name="ClusterId">The cluster id.</param>
/// <param name="Version">The version.</param>
/// <param name="Timestamp">The timestamp.</param>
internal record MembershipVersion(string ClusterId, int Version, DateTime Timestamp);
