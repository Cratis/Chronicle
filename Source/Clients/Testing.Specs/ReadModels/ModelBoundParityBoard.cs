// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Model-bound read model exercising set once and ordered children.
/// </summary>
/// <param name="Id">The board identifier.</param>
/// <param name="FirstTitle">The first title the board got - set once.</param>
/// <param name="Title">The current title.</param>
/// <param name="Entries">Entries, newest first.</param>
/// <param name="EntriesByName">Entries sorted by name.</param>
[FromEvent<ParityBoardTitled>]
public record ModelBoundParityBoard(
    [Key] Guid Id,
    [SetFrom<ParityBoardTitled>(nameof(ParityBoardTitled.Title), SetOnce = true)] string FirstTitle,
    string Title,
    [ChildrenFrom<ParityEntryAdded>(key: nameof(ParityEntryAdded.EntryId), Prepend = true)] IEnumerable<ModelBoundParityEntry> Entries,
    [ChildrenFrom<ParityRankedEntryAdded>(key: nameof(ParityRankedEntryAdded.EntryId), OrderBy = nameof(ModelBoundParityEntry.Name))] IEnumerable<ModelBoundParityEntry> EntriesByName);

/// <summary>
/// An entry on a <see cref="ModelBoundParityBoard"/>.
/// </summary>
/// <param name="EntryId">The entry identifier.</param>
/// <param name="Name">The entry name.</param>
/// <param name="Rank">The entry rank.</param>
public record ModelBoundParityEntry([Key] Guid EntryId, string Name, int Rank);
