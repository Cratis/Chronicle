// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Classifies the resources in use into the owners that share a resource with another owner.
/// </summary>
public static class ResourceNameClassifier
{
    /// <summary>
    /// Classify the resources in use.
    /// </summary>
    /// <param name="uses">The <see cref="ResourceUse"/> items read from every inventory.</param>
    /// <param name="exemptions">The <see cref="ResourceNameExemptions"/>.</param>
    /// <returns>The <see cref="ResourceNameAuditResult"/>.</returns>
    /// <remarks>
    /// Resources are grouped by their ordinal identity: each provider has already applied its own equality.
    /// The key of the grain addressing each owner is added here, since it is the same for every backend.
    /// Names the kernel keeps for itself and the System event store are never quarantined.
    /// </remarks>
    public static ResourceNameAuditResult Classify(IEnumerable<ResourceUse> uses, ResourceNameExemptions exemptions)
    {
        var all = uses.ToList();
        all.AddRange(all
            .Select(_ => _.Owner)
            .OfType<ResourceOwner>()
            .Distinct()
            .Select(_ => new ResourceUse(_, ResourceKind.GrainKey, "grain:" + (_.Namespace is null ? KeyHelper.Combine(_.EventStore) : KeyHelper.Combine(_.EventStore, _.Namespace)))).ToList());

        var stores = new HashSet<string>(StringComparer.Ordinal);
        var namespaces = new HashSet<ResourceOwner>();
        var overlaps = new List<ResourceOverlap>();

        foreach (var group in all.GroupBy(_ => _.Identity, StringComparer.Ordinal))
        {
            var owners = group.Select(_ => _.Owner).Distinct().ToList();
            if (owners.Count < 2 || IsExempt(owners, exemptions))
            {
                continue;
            }

            overlaps.Add(new ResourceOverlap(group.Key, owners));
            foreach (var owner in owners.OfType<ResourceOwner>().Where(_ => !IsSystem(_)))
            {
                if (owner.Namespace is null)
                {
                    stores.Add(owner.EventStore);
                }
                else
                {
                    namespaces.Add(owner);
                }
            }
        }

        return new ResourceNameAuditResult([.. stores], [.. namespaces], overlaps);
    }

    /// <summary>
    /// Check whether an owner is the System event store, or one of its namespaces.
    /// </summary>
    /// <param name="owner">The <see cref="ResourceOwner"/>.</param>
    /// <returns>True if it belongs to the System event store.</returns>
    public static bool IsSystem(ResourceOwner owner) => owner.EventStore == EventStoreName.System.Value;

    static bool IsExempt(List<ResourceOwner?> owners, ResourceNameExemptions exemptions) =>
        owners.TrueForAll(_ => _ is not null && (_.Namespace is null ? exemptions.IsExempt(_.EventStore) : exemptions.IsExempt(_.EventStore, _.Namespace)));
}
