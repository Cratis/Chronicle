// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Storage.InMemory.Projections.for_ProjectionDefinitionHistoryStorage.given;

public class a_history_storage : Specification
{
    protected IProjectionDefinitionHistoryStorage _storage;
    protected ProjectionDefinitionHistoryEntry _entry;
    protected ProjectionDefinitionHistoryEntry[] _entries;

    void Establish()
    {
        _storage = new ProjectionDefinitionHistoryStorage();
        var definition = new ProjectionDefinition(ProjectionOwner.Server, EventSequenceId.Log, "projection", "model", true, true, new(), new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>()) { Version = "1.0.0" };
        _entry = new(definition.Identifier, new string('A', 64), "definition-content-sha256/v1", definition.Version, DateTimeOffset.FromUnixTimeSeconds(100), definition);
    }

    protected async Task Read() => _entries = (await _storage.GetFor(_entry.Projection)).ToArray();
}
