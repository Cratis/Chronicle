// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the ownership claim an observer job runs under: the kind of work holding it and, for partition work, the partition.
/// </summary>
/// <param name="Holder">The <see cref="ClaimHolderKind"/> holding the claim; <see cref="ClaimHolderKind.None"/> for a job that runs under no claim.</param>
/// <param name="Partition">The partition claimed, or null for an observer-wide claim.</param>
public record ObserverJobScope(ClaimHolderKind Holder, Key? Partition)
{
    /// <summary>
    /// Gets the scope of a job that runs under no claim.
    /// </summary>
    public static readonly ObserverJobScope None = new(ClaimHolderKind.None, null);

    /// <summary>
    /// Gets whether the job runs under a claim.
    /// </summary>
    public bool IsClaimed => Holder != ClaimHolderKind.None;

    /// <summary>
    /// Gets the scope a request runs under.
    /// </summary>
    /// <param name="request">The <see cref="IObserverJobRequest"/>.</param>
    /// <returns>The <see cref="ObserverJobScope"/>.</returns>
    /// <remarks>
    /// A retry of a failed partition shares its base with the partition catch-up and replay requests, but runs under no claim.
    /// </remarks>
    public static ObserverJobScope Of(IObserverJobRequest request) => request switch
    {
        CatchUpObserverRequest => new(ClaimHolderKind.ObserverCatchUp, null),
        ReplayObserverRequest => new(ClaimHolderKind.ObserverReplay, null),
        CatchUpObserverPartitionRequest partition => new(ClaimHolderKind.PartitionCatchUp, partition.Key),
        ReplayObserverPartitionRequest partition => new(ClaimHolderKind.PartitionReplay, partition.Key),
        _ => None
    };

    /// <summary>
    /// Gets whether a request carries an ownership epoch that is no longer its scope's claim in observer state, so the
    /// job running it is refused when it starts or resumes.
    /// </summary>
    /// <param name="request">The <see cref="IObserverJobRequest"/>.</param>
    /// <param name="state">The <see cref="ObserverState"/> holding the persisted claims.</param>
    /// <returns>True if the request carries an epoch its scope no longer holds; false if it carries none or the current one.</returns>
    public static bool IsObsolete(IObserverJobRequest request, ObserverState state)
    {
        var scope = Of(request);
        return request.OwnershipEpoch is ulong epoch && scope.IsClaimed && scope.EpochIn(state)?.Value != epoch;
    }

    /// <summary>
    /// Gets the epoch of the claim this scope holds in persisted observer state.
    /// </summary>
    /// <param name="state">The <see cref="ObserverState"/>.</param>
    /// <returns>The <see cref="ClaimEpoch"/>, or null when nothing of this kind holds the scope.</returns>
    public ClaimEpoch? EpochIn(ObserverState state) => Partition is null
        ? EpochOf(state.ObserverWideClaim?.Holder, state.ObserverWideClaim?.Epoch)
        : EpochOf((state.PartitionClaims ?? []).FirstOrDefault(_ => _.Partition == Partition));

    ClaimEpoch? EpochOf(PartitionClaim? claim) => EpochOf(claim?.Holder, claim?.Epoch);

    ClaimEpoch? EpochOf(ClaimHolderKind? holder, ClaimEpoch? epoch) => holder == Holder ? epoch : null;
}
