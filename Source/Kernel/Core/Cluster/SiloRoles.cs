// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Configuration;
using Cratis.Monads;
using Orleans.Runtime.MembershipService.SiloMetadata;

namespace Cratis.Chronicle.Cluster;

/// <summary>
/// Reads Chronicle roles from the Orleans silo metadata cache.
/// </summary>
/// <param name="metadataCache">The silo metadata cache.</param>
public class SiloRoles(ISiloMetadataCache metadataCache) : ISiloRoles
{
    /// <summary>
    /// The silo metadata key for the role metadata format version.
    /// </summary>
    internal const string VersionKey = "cratis.chronicle.roles.v";

    /// <summary>
    /// The silo metadata key for the event sequences role.
    /// </summary>
    internal const string EventSequencesKey = "cratis.chronicle.role.eventSequences";

    /// <summary>
    /// The silo metadata key for the observers role.
    /// </summary>
    internal const string ObserversKey = "cratis.chronicle.role.observers";

    /// <summary>
    /// The silo metadata key for the jobs role.
    /// </summary>
    internal const string JobsKey = "cratis.chronicle.role.jobs";

    /// <inheritdoc/>
    public Option<ClusterRoles> GetRoles(SiloAddress siloAddress)
    {
        var metadata = metadataCache.GetSiloMetadata(siloAddress).Metadata;
        if (!metadata.TryGetValue(VersionKey, out var version) || version != "1")
        {
            return Option<ClusterRoles>.None();
        }

        if (!TryGetFlag(metadata, EventSequencesKey, out var hasEventSequences))
        {
            return Option<ClusterRoles>.None();
        }

        if (!TryGetFlag(metadata, ObserversKey, out var hasObservers))
        {
            return Option<ClusterRoles>.None();
        }

        if (!TryGetFlag(metadata, JobsKey, out var hasJobs))
        {
            return Option<ClusterRoles>.None();
        }

        return new ClusterRoles { EventSequences = hasEventSequences, Observers = hasObservers, Jobs = hasJobs };
    }

    static bool TryGetFlag(ImmutableDictionary<string, string> metadata, string key, out bool value)
    {
        value = false;
        return metadata.TryGetValue(key, out var text) && bool.TryParse(text, out value);
    }
}
