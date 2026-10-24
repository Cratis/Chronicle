// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Describes an atomic publication or restoration of a materialization generation.
/// </summary>
/// <param name="Key">The logical materialization.</param>
/// <param name="Active">The expected active generation.</param>
/// <param name="Epoch">The expected serving epoch.</param>
/// <param name="Candidate">The generation to activate.</param>
/// <param name="RollbackWindow">The displaced generation's rollback window.</param>
/// <param name="Receipt">The idempotency receipt.</param>
/// <param name="Restore">Whether this restores a retired generation.</param>
public sealed record MaterializationPublicationRequest(
    MaterializationKey Key,
    MaterializationGeneration Active,
    ServingEpoch Epoch,
    MaterializationGeneration Candidate,
    TimeSpan RollbackWindow,
    PublicationReceiptId Receipt,
    bool Restore)
{
    /// <summary>
    /// Decides whether the binding can be changed by this request.
    /// </summary>
    /// <param name="binding">The current authoritative binding.</param>
    /// <param name="now">The time of the attempted change.</param>
    /// <returns>The publication outcome.</returns>
    public MaterializationPublication Decide(MaterializationBinding binding, DateTimeOffset now)
    {
        if ((binding.Active, binding.Epoch) != (Active, Epoch))
        {
            return new(MaterializationPublicationResult.StaleBinding, binding.Active, binding.Epoch);
        }
        var candidate = binding.Generations.FirstOrDefault(_ => _.Identity.Generation == Candidate);
        var ready = Restore ? CanRestore(binding, candidate, now) : candidate?.State == MaterializationGenerationState.Following;
        if (!ready)
        {
            return new(Restore ? MaterializationPublicationResult.CandidateNotRestorable : MaterializationPublicationResult.CandidateNotFollowing, binding.Active, binding.Epoch);
        }
        return new(MaterializationPublicationResult.Published, Candidate, Epoch.Next());
    }

    /// <summary>
    /// Applies a successful publication's lifecycle changes to one generation.
    /// </summary>
    /// <param name="record">The generation record.</param>
    /// <param name="now">The publication time.</param>
    /// <returns>The updated record.</returns>
    public MaterializationGenerationRecord Apply(MaterializationGenerationRecord record, DateTimeOffset now)
    {
        if (record.Identity.Generation == Candidate)
        {
            return Restore
                ? record with { State = MaterializationGenerationState.Active, PromotedAt = now, RolledBackFrom = Active, RetiredAt = null, RollbackDeadline = null }
                : record with { State = MaterializationGenerationState.Active, PromotedAt = now };
        }
        return record.Identity.Generation == Active
            ? record with { State = MaterializationGenerationState.Retired, SupersededBy = Candidate, RetiredAt = now, RollbackDeadline = now + RollbackWindow }
            : record;
    }

    bool CanRestore(MaterializationBinding binding, MaterializationGenerationRecord? candidate, DateTimeOffset now) =>
        candidate?.IsRollbackEligible(now) == true &&
        candidate.State.CanTransitionTo(MaterializationGenerationState.Active) &&
        binding.Generations.Any(_ => _.Identity.Generation == Active && _.State.CanTransitionTo(MaterializationGenerationState.Retired));
}
