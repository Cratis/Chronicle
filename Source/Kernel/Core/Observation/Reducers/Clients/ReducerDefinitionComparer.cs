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
        if (!await storage.GetEventStore(reducerKey.EventStore).Reducers.Has(reducerKey.ReducerId))
        {
            return ReducerDefinitionCompareResult.New;
        }

        var firstEventTypes = first.EventTypes.Select(_ => _.EventType.Id).Order().ToArray();
        var secondEventTypes = second.EventTypes.Select(_ => _.EventType.Id).Order().ToArray();
        if (firstEventTypes.SequenceEqual(secondEventTypes) && first.IsActive == second.IsActive)
        {
            return ReducerDefinitionCompareResult.Same;
        }

        return ReducerDefinitionCompareResult.Different;
    }
}
