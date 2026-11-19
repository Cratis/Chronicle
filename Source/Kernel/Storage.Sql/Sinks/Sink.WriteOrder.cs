// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Applies the changes of a multi-write cycle in changeset order, so a Join that follows a direct change on the same row
/// leaves its value, as the projection computed it.
/// </summary>
public partial class Sink
{
    /// <summary>
    /// Splits the changes of a cycle by the order they were produced in. Joins and child removals before the first direct
    /// change go first. The first run of direct changes is the write that creates the row or guards it. What follows
    /// alternates between runs of joins and child removals and runs of direct changes.
    /// </summary>
    /// <param name="changes">The changes of the changeset, in order.</param>
    /// <returns>The changes split by when they are written.</returns>
    static CycleWriteOrder OrderWrites(IEnumerable<Change> changes)
    {
        var ordered = changes.ToArray();
        var leading = ordered.TakeWhile(IsJoinOrChildRemoval).ToArray();
        var fromFirstDirect = ordered.Skip(leading.Length).ToArray();
        var firstDirect = fromFirstDirect.TakeWhile(change => !IsJoinOrChildRemoval(change)).ToArray();
        return new(leading, firstDirect, GroupRuns(fromFirstDirect.Skip(firstDirect.Length)));
    }

    static bool IsJoinOrChildRemoval(Change change) => change is Joined or ChildRemovedFromAll;

    static Change[][] GroupRuns(IEnumerable<Change> changes)
    {
        var runs = new List<List<Change>>();
        foreach (var change in changes)
        {
            if (runs.Count > 0 && IsJoinOrChildRemoval(runs[^1][0]) == IsJoinOrChildRemoval(change))
            {
                runs[^1].Add(change);
            }
            else
            {
                runs.Add([change]);
            }
        }

        return [.. runs.Select(run => run.ToArray())];
    }

    async Task ApplyTrailingRuns(DbContextScope<ReadModelDbContext> scope, Key key, string id, EventSequenceNumber sequenceNumber, Change[][] trailing, bool directApplied)
    {
        foreach (var run in trailing)
        {
            if (IsJoinOrChildRemoval(run[0]))
            {
                await ApplyJoinsAndChildRemovals(scope, run, sequenceNumber, key.ArrayIndexers);
            }
            else if (directApplied)
            {
                var entry = await GetOrAttachEntity(scope, id, key);
                ApplyChangesToEntity(entry, run, key.ArrayIndexers);
                await scope.DbContext.SaveChangesAsync();
            }
        }
    }

    /// <summary>
    /// The changes of a cycle split by when they are written.
    /// </summary>
    /// <param name="Leading">The joins and child removals before the first direct change, in order.</param>
    /// <param name="FirstDirect">The first run of direct changes.</param>
    /// <param name="Trailing">The runs after that, each of joins and child removals or of direct changes only.</param>
    sealed record CycleWriteOrder(Change[] Leading, Change[] FirstDirect, Change[][] Trailing);
}
