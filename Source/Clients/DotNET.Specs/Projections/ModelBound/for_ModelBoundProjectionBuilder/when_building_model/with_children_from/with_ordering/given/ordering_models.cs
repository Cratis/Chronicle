// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from.with_ordering.given;

[EventType]
public record EntryAdded(Guid EntryId, string Name, int Rank);

[EventType]
public record EntryImported(Guid EntryId, string Name, int Rank);

public record Entry([Key] Guid EntryId, string Name, int Rank);

public record AppendedEntries(
    [Key] Guid Id,
    [ChildrenFrom<EntryAdded>(key: nameof(EntryAdded.EntryId))] IEnumerable<Entry> Entries);

public record PrependedEntries(
    [Key] Guid Id,
    [ChildrenFrom<EntryAdded>(key: nameof(EntryAdded.EntryId), Prepend = true)] IEnumerable<Entry> Entries);

public record EntriesOrderedByName(
    [Key] Guid Id,
    [ChildrenFrom<EntryAdded>(key: nameof(EntryAdded.EntryId), OrderBy = nameof(Entry.Name))] IEnumerable<Entry> Entries);

public record EntriesOrderedByRankDescending(
    [Key] Guid Id,
    [ChildrenFrom<EntryAdded>(key: nameof(EntryAdded.EntryId), OrderByDescending = nameof(Entry.Rank))]
    [ChildrenFrom<EntryImported>(key: nameof(EntryImported.EntryId))]
    IEnumerable<Entry> Entries);

public record EntriesWithConflictingOrderings(
    [Key] Guid Id,
    [ChildrenFrom<EntryAdded>(key: nameof(EntryAdded.EntryId), Prepend = true)]
    [ChildrenFrom<EntryImported>(key: nameof(EntryImported.EntryId), OrderBy = nameof(Entry.Name))]
    IEnumerable<Entry> Entries);

public record EntriesOrderedByMissingProperty(
    [Key] Guid Id,
    [ChildrenFrom<EntryAdded>(key: nameof(EntryAdded.EntryId), OrderBy = "Missing")] IEnumerable<Entry> Entries);

public class a_model_bound_projection_builder_for_ordered_children : for_ModelBoundProjectionBuilder.given.a_model_bound_projection_builder
{
    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(EntryAdded), typeof(EntryImported)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }
}
