// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes;

public partial class EventTypesStorage
{
    /// <inheritdoc/>
    public Task<EventTypeRegistrationResult> RegisterIfAbsent(EventTypeToRegister eventType)
    {
        lock (_publishing)
        {
            _definitions.TryGetValue(eventType.Definition.Id, out var existing);
            var result = EventTypeRegistrationResult.Compare(eventType.Definition, existing?.Generations ?? []);
            if (result.Conflict is not null)
            {
                return Task.FromResult(result);
            }

            var definition = eventType.Definition with
            {
                Generations = eventType.Definition.Generations.Where(_ => result.AddedGenerations.Contains(_.Generation)).ToList()
            };
            var changed = ApplyDefinition(definition, RegisterMetadata(definition.Id, eventType.Source, eventType.Visibility, eventType.Origin));
            PublishIfChanged(changed);
            return Task.FromResult(result with { Changed = changed });
        }
    }
}
