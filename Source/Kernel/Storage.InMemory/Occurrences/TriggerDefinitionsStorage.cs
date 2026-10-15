// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences;

/// <summary>
/// Stores trigger definitions under a per-event-store lock.
/// </summary>
public class TriggerDefinitionsStorage : ITriggerDefinitionsStorage
{
    readonly object _lock = new();
    readonly Dictionary<TriggerId, TriggerDefinition> _definitions = [];

    /// <inheritdoc/>
    public Task<TriggerDefinition?> Get(TriggerId id)
    {
        lock (_lock)
        {
            return Task.FromResult(_definitions.GetValueOrDefault(id));
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<TriggerDefinition>> GetAll()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<TriggerDefinition>>(_definitions.Values.ToArray());
        }
    }

    /// <inheritdoc/>
    public Task<bool> TrySave(TriggerDefinition definition, long? expectedRevision)
    {
        lock (_lock)
        {
            var existing = _definitions.GetValueOrDefault(definition.Id);
            if (existing?.Revision != expectedRevision)
            {
                return Task.FromResult(false);
            }

            _definitions[definition.Id] = definition;
            return Task.FromResult(true);
        }
    }
}
