// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents what a client declares at registration about caching the read model state it last returned per partition.
/// </summary>
/// <remarks>
/// When enabled, the kernel may answer a delivery with an unchanged marker instead of the state, meaning the
/// state is the one the client returned for the partition on its previous reduce.
/// </remarks>
[ProtoContract]
public class StateCaching
{
    /// <summary>
    /// Gets or sets a value indicating whether the client caches the state it returned per partition and accepts an unchanged marker.
    /// </summary>
    [ProtoMember(1)]
    public bool Enabled { get; set; }
}
