// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Represents a request asking whether a named set of observers have durably applied through a target position.
/// </summary>
[ProtoContract]
public class AppliedThroughRequest
{
    /// <summary>
    /// Gets or sets the event store the observers belong to.
    /// </summary>
    [ProtoMember(1)]
    public string EventStore { get; set; }

    /// <summary>
    /// Gets or sets the namespace the observers belong to.
    /// </summary>
    [ProtoMember(2)]
    public string Namespace { get; set; }

    /// <summary>
    /// Gets or sets the event sequence the observers observe.
    /// </summary>
    [ProtoMember(3)]
    public string EventSequenceId { get; set; }

    /// <summary>
    /// Gets or sets the explicit set of observer identifiers to check. Unlike <see cref="WaitForObserverCompletionRequest"/>,
    /// this is never "every observer on the sequence" implicitly.
    /// </summary>
    [ProtoMember(4)]
    public IEnumerable<string> ObserverIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the target event sequence number every named observer must have durably applied through.
    /// </summary>
    [ProtoMember(5)]
    public ulong TargetEventSequenceNumber { get; set; }
}
