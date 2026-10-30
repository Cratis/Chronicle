// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerDefinitionComparer"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/>.</param>
public class ReducerDefinitionComparer(IStorage storage) : IReducerDefinitionComparer
{
    /// <inheritdoc/>
    public async Task<ReducerDefinitionCompareResult> Compare(
        ReducerKey reducerKey,
        ReducerDefinition first,
        ReducerDefinition second)
    {
        var reducers = storage.GetEventStore(reducerKey.EventStore).Reducers;
        if (!await reducers.Has(reducerKey.ReducerId))
        {
            return ReducerDefinitionCompareResult.New;
        }

        if (first.EventTypes is null)
        {
            // A grain activated before another namespace stored the shared definition has empty state; load what it would have read.
            first = await reducers.Get(reducerKey.ReducerId);
        }

        var firstEventTypes = first.EventTypes.OrderBy(_ => _.EventType.Id).ToArray();
        var secondEventTypes = second.EventTypes.OrderBy(_ => _.EventType.Id).ToArray();
        var definitionsAreEqual =
            ReducerDefinitionEvolution.HasSameConfiguration(first, second) &&
            firstEventTypes.SequenceEqual(secondEventTypes);

        return definitionsAreEqual
            ? ReducerDefinitionCompareResult.Same
            : ReducerDefinitionCompareResult.Different;
    }
}
