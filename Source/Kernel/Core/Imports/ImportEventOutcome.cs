// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents how one imported event settled when it was committed.
/// </summary>
/// <param name="Index">The index of the event in the import.</param>
/// <param name="EventType">The event type, as its identifier and generation.</param>
/// <param name="Kind">The <see cref="ImportEventOutcomeKind"/>.</param>
/// <param name="SequenceNumber">The sequence number the event has in the log, new or already stored.</param>
/// <param name="Reasons">Why the event was rejected, when it was.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Imports.ImportEventOutcome")]
public record ImportEventOutcome([Id(0)] long Index, [Id(1)] string EventType, [Id(2)] ImportEventOutcomeKind Kind, [Id(3)] EventSequenceNumber SequenceNumber, [Id(4)] IEnumerable<string> Reasons);
