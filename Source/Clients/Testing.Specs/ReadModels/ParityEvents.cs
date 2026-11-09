// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Emitted when a parity board gets a title.
/// </summary>
/// <param name="Title">The title.</param>
[EventType]
public record ParityBoardTitled(string Title);

/// <summary>
/// Emitted when a series is assigned to a slot on a parity board.
/// </summary>
/// <param name="Slot">The slot - the dictionary key.</param>
/// <param name="SeriesId">The series assigned to the slot.</param>
[EventType]
public record ParitySlotAssigned(string Slot, string SeriesId);

/// <summary>
/// Emitted when a slot is dropped from a parity board.
/// </summary>
/// <param name="Slot">The slot - the dictionary key.</param>
[EventType]
public record ParitySlotDropped(string Slot);

/// <summary>
/// Emitted when an entry is added to a parity board.
/// </summary>
/// <param name="EntryId">The entry identifier.</param>
/// <param name="Name">The entry name.</param>
/// <param name="Rank">The entry rank.</param>
[EventType]
public record ParityEntryAdded(Guid EntryId, string Name, int Rank);

/// <summary>
/// Emitted when a ranked entry is added to a parity board.
/// </summary>
/// <param name="EntryId">The entry identifier.</param>
/// <param name="Name">The entry name.</param>
/// <param name="Rank">The entry rank.</param>
[EventType]
public record ParityRankedEntryAdded(Guid EntryId, string Name, int Rank);

/// <summary>
/// Emitted when an entry on a parity board changes.
/// </summary>
/// <param name="EntryId">The entry identifier.</param>
/// <param name="Name">The new name.</param>
/// <param name="Rank">The new rank.</param>
[EventType]
public record ParityEntryChanged(Guid EntryId, string Name, int Rank);
