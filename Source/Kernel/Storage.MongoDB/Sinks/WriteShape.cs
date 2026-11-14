// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Describes how the changes of a changeset address the document at a key: whether the write targets the
/// document by its <c>_id</c>, whether it may create it and whether its root update must be suppressed.
/// </summary>
/// <param name="UsesJoinTargetsOnlyFilter">Whether the documents are matched by the join column rather than by <c>_id</c>.</param>
/// <param name="IsUpsert">Whether the write may create the document.</param>
/// <param name="ShouldSuppressRootUpdateAfterRootLevelJoin">Whether the direct root update has no safe target after a root-level join.</param>
internal sealed record WriteShape(bool UsesJoinTargetsOnlyFilter, bool IsUpsert, bool ShouldSuppressRootUpdateAfterRootLevelJoin)
{
    /// <summary>
    /// Derives the <see cref="WriteShape"/> of a changeset written at a key.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> the changeset is written at.</param>
    /// <param name="changeset">The changeset to write.</param>
    /// <returns>The <see cref="WriteShape"/>.</returns>
    public static WriteShape For(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var changes = changeset.Changes;
        var hasDirectKeyScopedChanges = changes.Any(IsDirectKeyScopedChange);
        var hasConstructiveChanges = changes.Any(IsConstructiveChange);

        // When the event was consumed by a Join (Children Join<TEvent>) AND the only direct
        // key-scoped changes are PropertiesChanged (no ChildAdded / ChildRemoved that would
        // legitimately construct a document at this key), the upsert keyed on the join value
        // would create a phantom document. The classic case: a Group projection with
        // FromEvery.Set(LastUpdated) + Children.Join<UserCreated>. When UserCreated arrives
        // for a UserId that no Group has, the FromEvery PropertiesChanged would otherwise
        // upsert a phantom Group keyed on UserId. Use the join-targets-only filter (Empty)
        // in this case so only existing documents are updated.
        var hasJoined = changeset.HasJoined();
        var hasActualRootLevelJoin = HasActualRootLevelJoin(changes);
        var hasKeyedFrom = changes.OfType<Joined>().Any(joined => joined.HasKeyedFrom);

        // A From and a Join can consume the same event. Only a From owns the direct keyed write;
        // a join-only event's direct properties have no safe _id target and must still be suppressed.
        var onlyPropertyUpdatesAlongsideJoin = hasJoined && hasDirectKeyScopedChanges && !hasConstructiveChanges && !hasKeyedFrom;
        var shouldSuppressRootUpdateAfterRootLevelJoin = hasActualRootLevelJoin && onlyPropertyUpdatesAlongsideJoin;

        // The _id filter value is only computed when the document is actually keyed by _id. For a join whose
        // target documents are matched by the join column, the resolved key carries the JOIN VALUE — which for
        // a differently-typed read model key cannot be converted to the _id type and would freeze the partition.
        var usesJoinTargetsOnlyFilter = (hasJoined && !hasDirectKeyScopedChanges) || onlyPropertyUpdatesAlongsideJoin;

        // A ROOT-level join (no array indexers) never CONSTRUCTS a root document — it only enriches an
        // existing root matched by the join column; the root's own key is set by its From/[FromEvent] source.
        // Upserting on the resolved key for a root join would materialize a phantom root keyed by the JOIN
        // VALUE, which for a differently-typed read model key is stored with a string _id and freezes the
        // partition the moment a later read coerces it back to the key type. A CHILD join (has array
        // indexers) still upserts so it can construct the child structure regardless of seed order.
        var isRootLevelJoin = hasJoined && !key.ArrayIndexers.All.Any();

        var isUpsert = !OnlyRemovesChildren(changes) && IsUpsertWrite(onlyPropertyUpdatesAlongsideJoin, isRootLevelJoin, hasKeyedFrom);

        return new(usesJoinTargetsOnlyFilter, isUpsert, shouldSuppressRootUpdateAfterRootLevelJoin);
    }

    static bool IsDirectKeyScopedChange(Change change) =>
        change is PropertiesChanged<ExpandoObject> or ChildAdded or ChildRemoved;

    static bool IsConstructiveChange(Change change) =>
        change is ChildAdded or ChildRemoved;

    static bool HasActualRootLevelJoin(IEnumerable<Change> changes) =>
        changes
            .OfType<Joined>()
            .Any(joined => !joined.ArrayIndexers.All.Any());

    // Pulling a child from an absent root must not recreate that root. A changeset that also adds a child
    // can still construct it, but a removal without an addition only updates an existing document.
    static bool OnlyRemovesChildren(IEnumerable<Change> changes) =>
        changes.Any(change => change is ChildRemoved) && !changes.Any(change => change is ChildAdded);

    static bool IsUpsertWrite(bool onlyPropertyUpdatesAlongsideJoin, bool isRootLevelJoin, bool hasKeyedFrom) =>
        !onlyPropertyUpdatesAlongsideJoin && (!isRootLevelJoin || hasKeyedFrom);
}
