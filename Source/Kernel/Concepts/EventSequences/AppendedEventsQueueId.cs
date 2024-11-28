// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences;

/// <summary>
/// Represents the identifier of a queue for appended events.
/// </summary>
/// <param name="Value">The actual value.</param>
public record AppendedEventsQueueId(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// The identifier when not set.
    /// </summary>
    public static readonly AppendedEventsQueueId NotSet = -1;

    public static implicit operator AppendedEventsQueueId(int value) => new(value);
}
