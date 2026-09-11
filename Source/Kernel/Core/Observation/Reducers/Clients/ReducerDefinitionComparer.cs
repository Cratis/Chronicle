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

        var firstEventTypes = first.EventTypes.OrderBy(_ => _.EventType.Id).ToArray();
        var secondEventTypes = second.EventTypes.OrderBy(_ => _.EventType.Id).ToArray();
        var definitionsAreEqual =
            first.EventSequenceId == second.EventSequenceId &&
            first.ReadModel == second.ReadModel &&
            first.IsActive == second.IsActive &&
            first.Hash == second.Hash &&
            Equals(first.Filters, second.Filters) &&
            first.Tags.Order().SequenceEqual(second.Tags.Order()) &&
            firstEventTypes.SequenceEqual(secondEventTypes);

        return definitionsAreEqual
            ? ReducerDefinitionCompareResult.Same
            : ReducerDefinitionCompareResult.Different;
    }
}
