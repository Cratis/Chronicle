// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Decides when the composite keys of a read model have to be rebuilt in the current key format, and how.
/// </summary>
internal static class ReadModelKeyFormatMigration
{
    /// <summary>
    /// Determines whether Chronicle may start the key format migration replay by itself.
    /// </summary>
    /// <param name="policy">The configured definition evolution policy.</param>
    /// <param name="scope">The scope of the projection that owns the sink being migrated.</param>
    /// <returns>True when a replay may be started automatically, false when it is only recommended.</returns>
    /// <remarks>
    /// Only the <see cref="DefinitionEvolutionPolicy.Automatic"/> policy replays automatically. A projection with a shared global sink never does,
    /// because a namespace-local replay of its shared sink is not supported.
    /// </remarks>
    internal static bool CanReplay(DefinitionEvolutionPolicy policy, ProjectionScope scope) =>
        policy.Allows(DefinitionEvolutionOperation.FullReplay) && scope is not (ProjectionScope.Global or ProjectionScope.Both);

    /// <summary>
    /// Determines whether the sink of a read model holds composite keys in a legacy key format that only a replay can rebuild.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to find the sink in.</param>
    /// <param name="eventStore">The event store the read model belongs to.</param>
    /// <param name="namespace">The namespace of the sink, or <see cref="EventStoreNamespaceName.NotSet"/> for the global sink.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> to check.</param>
    /// <returns>True when a replay is required, false if not or if the sink cannot tell.</returns>
    internal static async Task<bool> IsRequired(IStorage storage, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel)
    {
        var sink = await storage.GetEventStore(eventStore).GetNamespace(@namespace).Sinks.GetFor(readModel);
        return sink is ICanReportKeyFormatMigration reporter && await reporter.RequiresKeyFormatMigration();
    }
}
