// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

public partial class ConstraintIndexReplay
{
    UniqueKey?[]? _uniqueCollectKeys;
    ClosingKey?[]? _closingCollectKeys;
    int _uniqueCollectIndex;
    int _closingCollectIndex;

    async Task CollectDifferences(CancellationToken cancellationToken)
    {
        _uniqueCollectKeys ??= _unique.Keys.ToArray();
        while (_uniqueCollectIndex < _uniqueCollectKeys.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = _uniqueCollectKeys[_uniqueCollectIndex]!;
            var state = _unique[key];
            _operationConstraint = key.Name;
            _operationPosition = state.Position;
            var current = await CurrentValues(key, state, cancellationToken);
            _unique.Remove(key);
            var retained = key.Retained;
            var matches = state.Value is null ? current.Length == 0 : current.Length == 1 && current[0] == state.Value;
            if (retained is not null) matches = current.Contains(retained) == (state.Value is not null);
            if (!matches)
            {
                Func<CancellationToken, Task>? release = null;
                if (retained is not null && current.Contains(retained))
                {
                    release = token => unique.RemoveValue(key.Source, state.Definition, retained, key.Scope, token);
                }
                else if (retained is null && current.Length > 0)
                {
                    release = token => unique.Remove(key.Source, state.Definition, key.Scope, token);
                }
                Func<CancellationToken, Task>? claim = state.Value is { } value
                    ? token => unique.Save(key.Source, state.Definition, state.Position, value, key.Scope, token)
                    : null;
                _differences.Add(new(key.Name, state.Position, release, claim, key.Scope, state.Value, current));
            }
            _uniqueCollectKeys[_uniqueCollectIndex++] = null;
            TrackKeys();
        }
        _closingCollectKeys ??= _closed.Keys.ToArray();
        while (_closingCollectIndex < _closingCollectKeys.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = _closingCollectKeys[_closingCollectIndex]!;
            var state = _closed[key];
            _operationConstraint = key.Name;
            _operationPosition = state.Position;
            var current = await closed.GetFor(key.Name.Value, key.Scope, cancellationToken).WaitAsync(cancellationToken);
            _closed.Remove(key);
            if (current?.SequenceNumber != state.Closure?.SequenceNumber)
            {
                var final = state.Closure;
                Func<CancellationToken, Task> claim = final is { } closure
                    ? token => closed.Close(closure, token)
                    : token => closed.Reopen(key.Name.Value, key.Scope, token);
                _differences.Add(new(key.Name, state.Position, null, claim, string.Empty, null, []));
            }
            _closingCollectKeys[_closingCollectIndex++] = null;
            TrackKeys();
        }
    }

    async Task<UniqueConstraintValue[]> CurrentValues(UniqueKey key, UniqueState state, CancellationToken cancellationToken)
    {
        if (key.Retained is not { } value)
        {
            return (await unique.GetFor(key.Source, state.Definition, key.Scope, cancellationToken).WaitAsync(cancellationToken)).ToArray();
        }

        var (allowed, position) = await unique.IsAllowed(key.Source, state.Definition, value, key.Scope, cancellationToken).WaitAsync(cancellationToken);
        return allowed && position != EventSequenceNumber.Unavailable ? [value] : [];
    }

    void ResetCollection()
    {
        _uniqueCollectKeys = null;
        _closingCollectKeys = null;
        _uniqueCollectIndex = 0;
        _closingCollectIndex = 0;
    }
}
