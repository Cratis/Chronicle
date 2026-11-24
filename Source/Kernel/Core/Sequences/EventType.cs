// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the payload of an event type.
/// </summary>
/// <param name="Id">The unique identifier of the event type.</param>
/// <param name="Generation">The generation of the event type.</param>
/// <param name="Tombstone">Whether or not the event is a tombstone event.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Sequences.EventType")]
public record EventType(
    [Id(0)] string Id,
    [Id(1)] uint Generation,
    [Id(2)] bool Tombstone);
