// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Stores the kernel-wide lease and diagnostic identity.
/// </summary>
/// <param name="Name">The Name.</param>
/// <param name="Owner">The Owner.</param>
/// <param name="ClusterId">The ClusterId.</param>
/// <param name="ServiceId">The ServiceId.</param>
/// <param name="HostName">The HostName.</param>
/// <param name="Version">The Version.</param>
/// <param name="AcquiredAt">The AcquiredAt.</param>
/// <param name="RenewedAt">The RenewedAt.</param>
/// <param name="ExpiresAt">The ExpiresAt.</param>
internal record SingleWriterLeaseRow(string Name, string Owner, string ClusterId, string ServiceId, string HostName, string Version, DateTime AcquiredAt, DateTime RenewedAt, DateTime ExpiresAt);
