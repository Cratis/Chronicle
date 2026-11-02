// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Holds the candidate rows of one update to an <see cref="InMemorySink"/> until all of them have been evaluated.
/// </summary>
/// <param name="published">The rows currently published, which the staged write reads but never changes.</param>
/// <remarks>
/// A row is copied the first time the update touches it, and every later change of the same update works on that
/// copy, so the keyed row, the rows a join fans out to and the rows a child is removed from never alias one another
/// or a published row. Nothing is visible to a reader until the candidates are published.
/// </remarks>
internal sealed class StagedWrite(IDictionary<object, ExpandoObject> published)
{
    readonly Dictionary<object, ExpandoObject> _candidates = [];
    readonly HashSet<object> _advancing = [];
    readonly List<object> _changedKeys = [];

    /// <summary>
    /// Gets the keys of the rows the write changes, in the order they were first changed.
    /// </summary>
    public IReadOnlyList<object> ChangedKeys => _changedKeys;

    /// <summary>
    /// Gets the key of the row the write removes, if any.
    /// </summary>
    public object? RemovedKey { get; private set; }

    /// <summary>
    /// Gets the keys of every row the write can see: the published rows followed by the rows only staged so far.
    /// </summary>
    /// <returns>A snapshot of the keys.</returns>
    public object[] RowKeys() => [.. published.Keys, .. _candidates.Keys.Where(key => !published.ContainsKey(key))];

    /// <summary>
    /// Gets the current state of a row for reading, staged or published.
    /// </summary>
    /// <param name="key">The row key.</param>
    /// <returns>The row, or null when there is none. The caller must not change it.</returns>
    public ExpandoObject? Current(object key) =>
        _candidates.TryGetValue(key, out var candidate) || published.TryGetValue(key, out candidate) ? candidate : null;

    /// <summary>
    /// Gets a candidate for a row: the staged one, or a copy of the published one until it is staged.
    /// </summary>
    /// <param name="key">The row key.</param>
    /// <returns>A candidate the caller may change and then <see cref="Stage"/>.</returns>
    public ExpandoObject CandidateFor(object key) =>
        _candidates.TryGetValue(key, out var candidate) ? candidate : published[key].Clone();

    /// <summary>
    /// Stages the candidate for a row.
    /// </summary>
    /// <param name="key">The row key.</param>
    /// <param name="candidate">The candidate, owned by the staged write from now on.</param>
    /// <param name="advancesWatermark">Whether publishing it moves the row's last handled event sequence number forward.</param>
    public void Stage(object key, ExpandoObject candidate, bool advancesWatermark)
    {
        _candidates[key] = candidate;
        if (advancesWatermark)
        {
            _advancing.Add(key);
        }

        if (!_changedKeys.Contains(key))
        {
            _changedKeys.Add(key);
        }
    }

    /// <summary>
    /// Stages the removal of a row.
    /// </summary>
    /// <param name="key">The row key.</param>
    public void StageRemoval(object key)
    {
        RemovedKey = key;
        _changedKeys.Add(key);
    }

    /// <summary>
    /// Gets the staged candidates.
    /// </summary>
    /// <returns>Each staged row with whether publishing it advances its watermark.</returns>
    public IEnumerable<(object Key, ExpandoObject Row, bool AdvancesWatermark)> Candidates() =>
        _candidates.Select(candidate => (candidate.Key, candidate.Value, _advancing.Contains(candidate.Key)));
}
