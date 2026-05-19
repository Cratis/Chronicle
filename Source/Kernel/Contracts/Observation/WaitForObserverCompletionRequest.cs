// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents a request to wait for observer completion for a specific append tail.
/// </summary>
[ProtoContract]
public class WaitForObserverCompletionRequest
{
    /// <summary>
    /// Gets or sets the event store where the append happened.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace where the append happened.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event sequence where the append happened.
    /// </summary>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the tail event sequence number from the append operation.
    /// </summary>
    [ProtoMember(4)]
    public ulong TailEventSequenceNumber { get; set; }
}
