// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents the response from fast forwarding an observer.
/// </summary>
[ProtoContract]
public class FastForwardResponse
{
    /// <summary>
    /// Gets or sets the <see cref="FastForwardOutcome"/> describing what happened.
    /// </summary>
    [ProtoMember(1)]
    public FastForwardOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets the next event sequence number the observer expected before the fast forward.
    /// </summary>
    [ProtoMember(2)]
    public ulong PreviousNextEventSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the next event sequence number the observer expects after the fast forward.
    /// </summary>
    [ProtoMember(3)]
    public ulong NextEventSequenceNumber { get; set; }
}
