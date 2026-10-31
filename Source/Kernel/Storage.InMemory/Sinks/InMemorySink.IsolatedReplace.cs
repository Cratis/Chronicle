// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

public partial class InMemorySink
{
    /// <inheritdoc/>
    public bool SupportsIsolatedReplace => true;

    /// <inheritdoc/>
    public Task<IsolatedReplaceResult> ReplaceIsolated(Key key, ExpandoObject? state, EventSequenceNumber watermark)
    {
        var keyValue = GetKeyValue(key);
        lock (_collectionLock)
        {
            if (_lastHandledEventSequenceNumbers.TryGetValue(keyValue, out var stored) && stored > watermark.Value)
            {
                return Task.FromResult(IsolatedReplaceResult.Superseded);
            }

            if (state is null)
            {
                _collection.Remove(keyValue);
                _lastHandledEventSequenceNumbers.Remove(keyValue);
            }
            else
            {
                var replacement = state.Clone();
                ((IDictionary<string, object?>)replacement)["id"] = key.Value;
                _collection[keyValue] = replacement;
                _lastHandledEventSequenceNumbers[keyValue] = watermark.Value;
            }
        }

        NotifyChanged(keyValue, wrotePrimary: true);
        return Task.FromResult(state is null ? IsolatedReplaceResult.Removed : IsolatedReplaceResult.Replaced);
    }

    Task<IEnumerable<FailedPartition>> ApplyRemoval(object key, EventSequenceNumber sequenceNumber, SinkWriteMode mode)
    {
        bool removedFromPrimary;
        lock (_collectionLock)
        {
            if (IsRejectedGuardedRemoval(key, sequenceNumber, mode))
            {
                return Task.FromResult<IEnumerable<FailedPartition>>([]);
            }
            Collection.Remove(key);
            LastHandledEventSequenceNumbers.Remove(key);
            removedFromPrimary = !_isReplaying;
        }
        NotifyChanged(key, removedFromPrimary);
        return Task.FromResult<IEnumerable<FailedPartition>>([]);
    }

    bool IsRejectedGuardedRemoval(object key, EventSequenceNumber sequenceNumber, SinkWriteMode mode) =>
        mode == SinkWriteMode.GuardedUpsert && sequenceNumber.IsActualValue && !AdvancesWatermark(key, sequenceNumber);

    bool IsRejectedWatermarkWrite(object key, EventSequenceNumber sequenceNumber, SinkWriteMode mode)
    {
        if (!sequenceNumber.IsActualValue || mode is not (SinkWriteMode.OnlyWhenAdvancingWatermark or SinkWriteMode.GuardedUpsert))
        {
            return false;
        }
        if (mode == SinkWriteMode.GuardedUpsert && !Collection.ContainsKey(key))
        {
            return false;
        }
        return !AdvancesWatermark(key, sequenceNumber);
    }
}
