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
    /// Splits the changes of a cycle by the order they were produced in. Joins before the first direct change and every child
    /// removal go first. The first run of direct changes is the write that creates the row or guards it. What follows
    /// alternates between runs of joins and runs of direct changes.
    /// </summary>
    /// <param name="changes">The changes of the changeset, in order.</param>
    /// <returns>The changes split by when they are written.</returns>
    static CycleWriteOrder OrderWrites(IEnumerable<Change> changes)
    {
        var ordered = changes.Where(change => change is not ChildRemovedFromAll).ToArray();
        var leading = changes.OfType<ChildRemovedFromAll>().Cast<Change>().Concat(ordered.TakeWhile(change => change is Joined)).ToArray();
        var fromFirstDirect = ordered.Skip(leading.Count(change => change is Joined)).ToArray();
        var firstDirect = fromFirstDirect.TakeWhile(change => change is not Joined).ToArray();
        return new(leading, firstDirect, GroupRuns(fromFirstDirect.Skip(firstDirect.Length)));
    }

    static Change[][] GroupRuns(IEnumerable<Change> changes)
    {
        var runs = new List<List<Change>>();
        foreach (var change in changes)
        {
            if (runs.Count > 0 && (runs[^1][0] is Joined) == (change is Joined))
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
            if (run[0] is Joined)
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
    /// <param name="Leading">The joins before the first direct change, and the child removals.</param>
    /// <param name="FirstDirect">The first run of direct changes.</param>
    /// <param name="Trailing">The runs after that, each of joins only or of direct changes only.</param>
    sealed record CycleWriteOrder(Change[] Leading, Change[] FirstDirect, Change[][] Trailing);
}
