// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Federation;

/// <summary>
/// Represents a pull request the acceptor sends to the dialer.
/// </summary>
[ProtoContract]
public class ServePullRequest
{
    /// <summary>
    /// Gets or sets the request id the dialer echoes in its answer.
    /// </summary>
    [ProtoMember(1)]
    public ulong RequestId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number to read from.
    /// </summary>
    [ProtoMember(2)]
    public ulong FromSequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the most events to return.
    /// </summary>
    [ProtoMember(3)]
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets the event types wanted. The dialer intersects them with what it serves.
    /// </summary>
    [ProtoMember(4)]
    public IEnumerable<string> EventTypes { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the status of the frame: empty or Ok for a request, Denied when the acceptor refuses this direction, which is final for the call.
    /// </summary>
    [ProtoMember(5)]
    public string Status { get; set; } = string.Empty;
}
