// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model exercising set once, dictionary entries keyed by event content and ordered children through a fluent projection.
/// </summary>
/// <param name="Id">The board identifier.</param>
/// <param name="FirstTitle">The first title the board got - set once.</param>
/// <param name="Title">The current title.</param>
/// <param name="Slots">Series by slot, keyed by the slot on the events.</param>
/// <param name="Entries">Entries, newest first.</param>
/// <param name="RankedEntries">Entries, highest rank first.</param>
public record ParityBoard(
    Guid Id,
    string FirstTitle,
    string Title,
    IDictionary<string, string> Slots,
    IEnumerable<ParityEntry> Entries,
    IEnumerable<ParityEntry> RankedEntries);

/// <summary>
/// An entry on a <see cref="ParityBoard"/>.
/// </summary>
/// <param name="EntryId">The entry identifier.</param>
/// <param name="Name">The entry name.</param>
/// <param name="Rank">The entry rank.</param>
public record ParityEntry(Guid EntryId, string Name, int Rank);

/// <summary>
/// Fluent projection for <see cref="ParityBoard"/>.
/// </summary>
public class ParityBoardProjection : IProjectionFor<ParityBoard>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<ParityBoard> builder) => builder
        .From<ParityBoardTitled>(_ => _
            .SetOnce(m => m.FirstTitle).To(e => e.Title)
            .Set(m => m.Title).To(e => e.Title))
        .From<ParitySlotAssigned>(_ => _.Set(m => m.Slots, e => e.Slot).To(e => e.SeriesId))
        .From<ParitySlotDropped>(_ => _.RemoveKey(m => m.Slots, e => e.Slot))
        .Children(m => m.Entries, children => children
            .IdentifiedBy(e => e.EntryId)
            .Prepend()
            .From<ParityEntryAdded>(from => from.UsingKey(e => e.EntryId))
            .From<ParityEntryChanged>(from => from.UsingKey(e => e.EntryId)))
        .Children(m => m.RankedEntries, children => children
            .IdentifiedBy(e => e.EntryId)
            .OrderByDescending(e => e.Rank)
            .From<ParityRankedEntryAdded>(from => from.UsingKey(e => e.EntryId))
            .From<ParityEntryChanged>(from => from.UsingKey(e => e.EntryId)));
}
