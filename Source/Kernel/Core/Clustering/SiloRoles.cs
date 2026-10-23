// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Monads;
using Orleans.Runtime;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Chronicle.Clustering;

/// <summary>
/// Reads Chronicle roles from the Orleans silo metadata cache.
/// </summary>
/// <param name="metadataCache">The silo metadata cache.</param>
public class SiloRoles(ISiloMetadataCache metadataCache) : ISiloRoles
{
    internal const string VersionKey = "cratis.chronicle.roles.v";
    internal const string EventSequencesKey = "cratis.chronicle.role.eventSequences";
    internal const string ObserversKey = "cratis.chronicle.role.observers";
    internal const string JobsKey = "cratis.chronicle.role.jobs";

    /// <inheritdoc/>
    public Option<ClusterRoles> GetRoles(SiloAddress siloAddress)
    {
        var metadata = metadataCache.GetSiloMetadata(siloAddress).Metadata;
        if (metadata.TryGetValue(VersionKey, out var version) && version == "1" &&
            metadata.TryGetValue(EventSequencesKey, out var eventSequences) && bool.TryParse(eventSequences, out var hasEventSequences) &&
            metadata.TryGetValue(ObserversKey, out var observers) && bool.TryParse(observers, out var hasObservers) &&
            metadata.TryGetValue(JobsKey, out var jobs) && bool.TryParse(jobs, out var hasJobs))
        {
            return new ClusterRoles { EventSequences = hasEventSequences, Observers = hasObservers, Jobs = hasJobs };
        }

        return Option<ClusterRoles>.None();
    }
}
