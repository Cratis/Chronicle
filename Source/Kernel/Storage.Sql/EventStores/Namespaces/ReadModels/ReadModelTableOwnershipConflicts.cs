// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Checks every known name interpretation without preferring one role or logical scope.
/// </summary>
/// <param name="catalog">The exact catalog for the resolved placement.</param>
internal sealed class ReadModelTableOwnershipConflicts(IReadModelTableCatalog catalog)
{
    /// <summary>
    /// Refuses the candidate when it equals a reserved name or another owner's claim.
    /// </summary>
    /// <param name="owner">The requested owner.</param>
    /// <param name="physical">The candidate physical name.</param>
    /// <param name="claims">Existing relation claims.</param>
    /// <param name="bindings">Committed bindings.</param>
    /// <returns>A task that completes when no conflict exists.</returns>
    /// <exception cref="ReadModelTableNameBindingFailure">The candidate conflicts.</exception>
    internal async Task Check(
        ReadModelTableOwner owner,
        string physical,
        IReadOnlyList<ReadModelTableClaim> claims,
        IReadOnlyList<ReadModelTableNameBinding> bindings)
    {
        foreach (var reserved in new[] { WellKnownTableNames.ReadModelTableNames, WellKnownTableNames.ReplayPromotions })
        {
            if (await catalog.Equal(physical, reserved))
            {
                throw new ReadModelTableNameBindingFailure(owner, physical, "reserved internal table name");
            }
        }

        foreach (var binding in bindings.Where(binding => binding.Owner != owner))
        {
            if (await catalog.Equal(physical, binding.PhysicalName))
            {
                throw new ReadModelTableNameBindingFailure(owner, physical, "physical name is permanently reserved", binding.Owner);
            }
        }

        foreach (var claim in claims.Where(claim => claim.Owner != owner))
        {
            var names = new[]
            {
                claim.Owner.LogicalName,
                ReadModelTableNames.Candidate(catalog.Provider, claim.Owner.LogicalName),
                ReadModelTableNames.Legacy(catalog.Provider, claim.Owner.LogicalName),
                claim.PromotedPhysicalName
            };
            foreach (var name in names.Where(name => name is not null))
            {
                if (await catalog.Equal(physical, name!))
                {
                    throw new ReadModelTableNameBindingFailure(owner, physical, "competing persisted metadata claimant", claim.Owner);
                }
            }
        }
    }
}
