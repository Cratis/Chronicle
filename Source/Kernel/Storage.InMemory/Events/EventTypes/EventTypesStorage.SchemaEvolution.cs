// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes;

public partial class EventTypesStorage
{
    /// <inheritdoc/>
    public Task<bool> TryReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema expected, JsonSchema schema)
    {
        lock (_publishing)
        {
            if (!_definitions.TryGetValue(eventTypeId, out var definition) ||
                Generation(definition, generation) is not { } stored || stored.Schema.ToJson() != expected.ToJson())
            {
                return Task.FromResult(false);
            }

            var updated = definition with
            {
                Generations = definition.Generations.Select(item => item.Generation == generation
                    ? item with { Schema = schema }
                    : item).ToArray()
            };
            if (!_definitions.TryUpdate(eventTypeId, updated, definition))
            {
                return Task.FromResult(false);
            }

            PublishIfChanged(true);
            return Task.FromResult(true);
        }
    }
}
