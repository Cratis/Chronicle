// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <content>
/// Converts consecutive direct changes without moving them across a join or a child removal.
/// </content>
public partial class ChangesetConverter
{
    /// <inheritdoc/>
    async IAsyncEnumerable<UpdateDefinitionAndArrayFilters> IChangesetConverterWithDirectWrites.ToUpdatesInOrder(
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        ChangesetWriteContext context)
    {
        await foreach (var updates in ConvertInOrder(context, changeset.Changes, changeset.InitialState))
        {
            yield return ToDefinition(updates, context.SequenceNumber);
        }
    }

    static IEnumerable<Change> FlattenResolvedJoins(IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            if (change is ResolvedJoin resolved)
            {
                foreach (var nested in FlattenResolvedJoins(resolved.Changes))
                {
                    yield return nested;
                }
            }
            else
            {
                yield return change;
            }
        }
    }

    async IAsyncEnumerable<UpdateAccumulator> ConvertInOrder(ChangesetWriteContext context, IEnumerable<Change> changes, ExpandoObject? initialState)
    {
        var direct = new List<Change>();
        foreach (var change in FlattenResolvedJoins(JoinedChangeNormalizer.Normalize(changes)))
        {
            if (change is not Joined and not ChildRemovedFromAll)
            {
                direct.Add(change);
                continue;
            }

            if (direct.Count > 0)
            {
                yield return await ConvertDirect(context, direct, initialState);
                direct.Clear();
            }

            if (change is Joined joined)
            {
                await PerformJoined(context, joined);
            }
            else if (change is ChildRemovedFromAll removed)
            {
                await context.RemoveChild(removed);
            }
        }

        if (direct.Count > 0)
        {
            yield return await ConvertDirect(context, direct, initialState);
        }
    }

    async Task<UpdateAccumulator> ConvertDirect(ChangesetWriteContext context, IEnumerable<Change> changes, ExpandoObject? initialState)
    {
        var updates = new UpdateAccumulator();
        await ApplyActualChanges(context, changes, initialState, updates);
        return updates;
    }
}
