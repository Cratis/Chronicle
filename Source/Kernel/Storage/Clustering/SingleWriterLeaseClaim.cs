// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.Storage.SingleWriter;

/// <summary>
/// A request to acquire the kernel storage lease.
/// </summary>
/// <param name="Owner">The Owner.</param>
/// <param name="Ttl">The Ttl.</param>
/// <param name="ClusterId">The ClusterId.</param>
/// <param name="ServiceId">The ServiceId.</param>
/// <param name="HostName">The HostName.</param>
/// <param name="Version">The Version.</param>
public record SingleWriterLeaseClaim(SingleWriterLeaseOwner Owner, TimeSpan Ttl, string ClusterId, string ServiceId, string HostName, string Version);
