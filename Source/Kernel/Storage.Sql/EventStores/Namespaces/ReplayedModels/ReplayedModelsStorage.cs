// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;

/// <summary>
/// Represents an implementation of <see cref="IReplayedReadModelsStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ReplayedModelsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IReplayedReadModelsStorage
{
    /// <inheritdoc/>
    public async Task Replayed(ObserverId observer, ReplayContext context)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        var occurrence = ReplayedModelsConverters.ToReplayedModelOccurrence(observer, context);
        scope.DbContext.ReplayedModels.Add(occurrence);
        await scope.DbContext.SaveChangesAsync();
    }
}
