// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.Storage.SingleWriter;

/// <summary>
/// The current holder, with timestamps from storage.
/// </summary>
/// <param name="Owner">The Owner.</param>
/// <param name="HostName">The HostName.</param>
/// <param name="Version">The Version.</param>
/// <param name="AcquiredAt">The AcquiredAt.</param>
/// <param name="ExpiresAt">The ExpiresAt.</param>
public record SingleWriterLeaseHolder(SingleWriterLeaseOwner Owner, string HostName, string Version, DateTimeOffset AcquiredAt, DateTimeOffset ExpiresAt);
