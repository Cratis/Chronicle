// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Holds information about a single event sequence.
/// </summary>
[ProtoContract]
public class EventSequenceInformation
{
    /// <summary>
    /// Gets or sets the unique identifier of the event sequence.
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable display name of the event sequence.
    /// </summary>
    [ProtoMember(2, IsRequired = true)]
    public string Name { get; set; }
}
