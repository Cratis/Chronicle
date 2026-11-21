// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Tracks the sequential result of the claims in one constraint scope.
/// </summary>
/// <param name="mode">The constraint's retention mode.</param>
/// <param name="initial">The persisted rows touched by the batch.</param>
internal sealed class UniqueConstraintBatchModel(UniqueConstraintMode mode, IEnumerable<UniqueConstraintBatchModel.Row> initial)
{
    readonly Dictionary<string, Row> _byValue = initial.ToDictionary(row => row.Value);
    readonly Dictionary<string, HashSet<string>> _bySource = initial.GroupBy(row => row.EventSourceId)
        .ToDictionary(group => group.Key, group => group.Select(row => row.Value).ToHashSet());

    /// <summary>
    /// Gets the final rows after applying the batch.
    /// </summary>
    internal IEnumerable<Row> Rows => _byValue.Values;

    /// <summary>
    /// Applies one change without modifying the model when another source owns the value.
    /// </summary>
    /// <param name="change">The next change.</param>
    /// <returns>Whether the change was accepted.</returns>
    internal bool Apply(UniqueConstraintIndexChange change)
    {
        switch (change.Kind)
        {
            case UniqueConstraintIndexChangeKind.Save:
                return Save(change);
            case UniqueConstraintIndexChangeKind.RemoveValue:
                if (_byValue.TryGetValue(change.Value!.Value, out var row) && row.EventSourceId == change.EventSourceId.Value)
                {
                    Remove(row);
                }
                break;
            default:
                RemoveSource(change.EventSourceId.Value);
                break;
        }
        return true;
    }

    bool Save(UniqueConstraintIndexChange change)
    {
        var source = change.EventSourceId.Value;
        var value = change.Value!.Value;
        _byValue.TryGetValue(value, out var existing);
        if (existing is not null && existing.EventSourceId != source)
        {
            return false;
        }

        decimal sequence = change.SequenceNumber.Value;
        if (mode == UniqueConstraintMode.PerValue)
        {
            sequence = existing?.SequenceNumber ?? sequence;
        }
        else
        {
            RemoveSource(source);
        }

        _byValue[value] = new Row(source, value, sequence, change.Subject?.Value);
        if (!_bySource.TryGetValue(source, out var values))
        {
            values = [];
            _bySource[source] = values;
        }
        values.Add(value);
        return true;
    }

    void RemoveSource(string source)
    {
        if (!_bySource.TryGetValue(source, out var values))
        {
            return;
        }
        foreach (var value in values)
        {
            _byValue.Remove(value);
        }
        _bySource.Remove(source);
    }

    void Remove(Row row)
    {
        _byValue.Remove(row.Value);
        _bySource[row.EventSourceId].Remove(row.Value);
    }

    /// <summary>
    /// Represents the persisted state of a claim.
    /// </summary>
    /// <param name="EventSourceId">The owner.</param>
    /// <param name="Value">The claimed value.</param>
    /// <param name="SequenceNumber">The stored sequence number.</param>
    /// <param name="Subject">The optional subject.</param>
    internal sealed record Row(string EventSourceId, string Value, decimal SequenceNumber, string? Subject);
}
