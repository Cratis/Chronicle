// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>>> GetLastInstancesOfAny(IEnumerable<EventSourceId> eventSourceIds, IEnumerable<EventTypeId> eventTypes)
    {
        var types = eventTypes.ToArray();
        var results = new Dictionary<EventSourceId, Option<AppendedEvent>>();
        foreach (var source in eventSourceIds.Distinct())
        {
            results[source] = await TryGetLastInstanceOfAny(source, types);
        }

        return results;
    }
}
