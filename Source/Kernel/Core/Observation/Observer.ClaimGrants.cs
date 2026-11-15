// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer : IObserverOwnershipClaims
{
    /// <summary>
    /// Counts the state writes that reached storage, so a grant can tell whether its write was persisted.
    /// </summary>
    ulong _persistedStateWrites;

    /// <summary>
    /// Whether the claims ledger was restored from persisted state. A ledger started over after a failed restore issues
    /// epochs from the beginning again, so none of its grants is stamped on a job.
    /// </summary>
    bool _claimsTrusted = true;

    /// <inheritdoc/>
    public async Task<ulong?> GrantObserverWideBeforeStart(ClaimHolderKind holder, EventSequenceNumber resumePosition)
    {
        // Catch-up never takes the observer from a replay; its job then runs unchecked, as before claims were enforced.
        if (!CanGrantClaims() || WouldTakeFromReplay(holder))
        {
            return null;
        }

        ClaimEpoch? epoch = null;
        GuardShadowClaims(() =>
        {
            ReportShadowDivergences(_shadowClaims.GrantObserverWide(holder, null, resumePosition));
            epoch = _shadowClaims.ObserverWide?.Epoch;
        });

        var scope = new ObserverJobScope(holder, null);
        return await PersistedEpochs([(scope, epoch)]) is { Count: 1 } persisted ? persisted[scope] : null;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Key, ulong?>> GrantPartitionsBeforeStart(IEnumerable<Key> partitions, ClaimHolderKind holder, EventSequenceNumber resumePosition)
    {
        var keys = partitions.ToArray();
        var set = holder == ClaimHolderKind.PartitionReplay ? State.ReplayingPartitions : State.CatchingUpPartitions;
        set.UnionWith(keys);
        _partitionHoldsVersion++;
        if (!CanGrantClaims())
        {
            await WriteStateAsync();
            return keys.ToDictionary(_ => _, _ => (ulong?)null);
        }

        var granted = keys.Select(partition =>
        {
            ClaimEpoch? epoch = null;
            GuardShadowClaims(() =>
            {
                ReportShadowDivergences(_shadowClaims.Grant(partition, holder, null, resumePosition));
                epoch = _shadowClaims.ClaimFor(partition)?.Epoch;
            });
            return (new ObserverJobScope(holder, partition), epoch);
        }).ToArray();

        var persisted = await PersistedEpochs(granted);
        return keys.ToDictionary(_ => _, _ => persisted.GetValueOrDefault(new ObserverJobScope(holder, _)));
    }

    /// <inheritdoc/>
    public void AttachObserverWideJob(ulong epoch, JobId job) =>
        GuardShadowClaims(() => _shadowClaims.AttachObserverWideJob(new(epoch), job));

    /// <inheritdoc/>
    public bool IsObsolete(IObserverJobRequest request) => ObserverJobScope.IsObsolete(request, State);

    /// <inheritdoc/>
    public bool IsHeldByNewerClaim(Key partition, ulong? epoch)
    {
        if (epoch is null)
        {
            return false;
        }

        // Only a newer catch-up of the partition reads the batch the refused one would have; a newer replay does not cover it.
        var current = new ObserverJobScope(ClaimHolderKind.PartitionCatchUp, partition).EpochIn(State);
        return State.CatchingUpPartitions.Contains(partition) && current is not null && current.Value > epoch.Value;
    }

    /// <summary>
    /// Begins starting a partition job, keeping hold reconciliation from releasing the partition before the job is listed.
    /// </summary>
    /// <returns>A <see cref="PartitionJobStart"/> that ends the start when disposed.</returns>
    PartitionJobStart BeginPartitionJobStart()
    {
        _liveHandOversInFlight++;
        _partitionHoldsVersion++;
        return new PartitionJobStart(this);
    }

    bool CanGrantClaims() => _claimsTrusted && !_removed && !IsRetired && !CannotWriteState();

    bool WouldTakeFromReplay(ClaimHolderKind holder) =>
        holder == ClaimHolderKind.ObserverCatchUp && _shadowClaims.ObserverWide?.Holder == ClaimHolderKind.ObserverReplay;

    /// <summary>
    /// Persists granted claims, returning the epoch of each claim the write left in persisted state.
    /// </summary>
    /// <param name="granted">The scopes granted and their epochs.</param>
    /// <returns>The persisted epoch per scope; a scope whose claim was not persisted is absent, and its job runs unchecked.</returns>
    /// <remarks>
    /// A write this activation may not make - the observer moved, or its state writing is suspended - persists nothing,
    /// and the job then runs unchecked, as before claims were enforced. A write that fails throws, as it did before.
    /// </remarks>
    async Task<IReadOnlyDictionary<ObserverJobScope, ulong?>> PersistedEpochs(IReadOnlyCollection<(ObserverJobScope Scope, ClaimEpoch? Epoch)> granted)
    {
        var writes = _persistedStateWrites;
        await WriteStateAsync();
        if (writes == _persistedStateWrites)
        {
            return new Dictionary<ObserverJobScope, ulong?>();
        }

        return granted
            .Where(_ => _.Epoch is not null && _.Scope.EpochIn(State) == _.Epoch)
            .ToDictionary(_ => _.Scope, _ => (ulong?)_.Epoch!.Value);
    }

    sealed class PartitionJobStart(Observer observer) : IDisposable
    {
        public void Dispose()
        {
            observer._liveHandOversInFlight--;
            observer._partitionHoldsVersion++;
        }
    }
}
