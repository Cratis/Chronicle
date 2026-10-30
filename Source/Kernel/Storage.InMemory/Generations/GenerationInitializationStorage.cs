// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Storage.InMemory.Generations;

/// <summary>
/// Keeps initialization evidence with first-write-wins receipt semantics.
/// </summary>
public sealed class GenerationInitializationStorage : IGenerationInitializationStorage
{
    readonly Lock _gate = new();
    readonly Dictionary<(GenerationInitializationStep Step, string Subject), GenerationInitializationReceipt> _receipts = new();
    readonly Dictionary<(GenerationInitializationBlockerKind Kind, string Subject), GenerationInitializationBlocker> _blockers = new();

    /// <summary>
    /// Gets whether initialization has recorded any receipt.
    /// </summary>
    public bool HasReceipts
    {
        get
        {
            lock (_gate)
            {
                return _receipts.Count != 0;
            }
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<GenerationInitializationReceipt>> GetReceipts()
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([.. _receipts.Values]);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryRecord(GenerationInitializationReceipt receipt)
    {
        lock (_gate)
        {
            return Task.FromResult(_receipts.TryAdd((receipt.Step, receipt.Subject), receipt));
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<GenerationInitializationBlocker>> GetBlockers()
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<GenerationInitializationBlocker>>([.. _blockers.Values]);
        }
    }

    /// <inheritdoc/>
    public Task RecordBlocker(GenerationInitializationBlocker blocker)
    {
        lock (_gate)
        {
            var key = (blocker.Kind, blocker.Subject);
            _blockers[key] = _blockers.TryGetValue(key, out var existing) && existing.ResolvedAt is null
                ? blocker with { RaisedAt = existing.RaisedAt }
                : blocker;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResolveBlocker(GenerationInitializationBlockerKind kind, string subject, DateTimeOffset at)
    {
        lock (_gate)
        {
            if (_blockers.TryGetValue((kind, subject), out var blocker) && blocker.ResolvedAt is null)
            {
                _blockers[(kind, subject)] = blocker with { ResolvedAt = at };
            }
        }
        return Task.CompletedTask;
    }
}
