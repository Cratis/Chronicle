// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents what a client declares at registration about receiving events as pages spanning several partitions.
/// </summary>
[ProtoContract]
public class PageDelivery
{
    /// <summary>
    /// Gets or sets a value indicating whether the client receives pages and answers with one result per partition.
    /// </summary>
    [ProtoMember(1)]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the most events the client wants in one page. Zero leaves it to the kernel.
    /// </summary>
    [ProtoMember(2)]
    public uint MaxEventsPerPage { get; set; }

    /// <summary>
    /// Gets or sets the most bytes the client wants in one page. Zero leaves it to the kernel.
    /// </summary>
    [ProtoMember(3)]
    public uint MaxBytesPerPage { get; set; }

    /// <summary>
    /// Gets or sets how long, in milliseconds, the kernel may wait to fill a live page. Zero sends what is waiting.
    /// </summary>
    [ProtoMember(4)]
    public uint MaxDelayMilliseconds { get; set; }
}
