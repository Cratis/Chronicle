// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reactors;

/// <summary>
/// Represents an implementation of <see cref="IReactorDefinitionComparer"/>.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/>.</param>
public class ReactorDefinitionComparer(IStorage storage) : IReactorDefinitionComparer
{
    /// <inheritdoc/>
    public async Task<ReactorDefinitionCompareResult> Compare(
        ReactorKey reactorKey,
        ReactorDefinition first,
        ReactorDefinition second)
    {
        if (!await storage.GetEventStore(reactorKey.EventStore).Reactors.Has(reactorKey.ReactorId))
        {
            return ReactorDefinitionCompareResult.New;
        }

        var firstEventTypes = first.EventTypes.Select(_ => _.EventType.Id).Order().ToArray();
        var secondEventTypes = second.EventTypes.Select(_ => _.EventType.Id).Order().ToArray();
        if (firstEventTypes.SequenceEqual(secondEventTypes))
        {
            return ReactorDefinitionCompareResult.Same;
        }

        return ReactorDefinitionCompareResult.Different;
    }
}
