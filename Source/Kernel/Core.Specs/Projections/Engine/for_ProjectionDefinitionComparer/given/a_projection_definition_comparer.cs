// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Projections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionDefinitionComparer.given;

public class a_projection_definition_comparer : Specification
{
    protected ProjectionDefinitionComparer _comparer;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IProjectionDefinitionsStorage _projectionsStorage;
    protected ProjectionKey _projectionKey;
    protected ObjectComparer _objectComparer;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _projectionsStorage = Substitute.For<IProjectionDefinitionsStorage>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.Projections.Returns(_projectionsStorage);

        _objectComparer = new ObjectComparer();
        _comparer = new ProjectionDefinitionComparer(_storage, _objectComparer, NullLogger<ProjectionDefinitionComparer>.Instance);

        _projectionKey = new ProjectionKey("test-projection", "test-event-store");
    }

    protected static ProjectionDefinition CreateDefinition(
        IDictionary<EventType, FromDefinition>? from = null,
        IDictionary<EventType, JoinDefinition>? join = null,
        IDictionary<PropertyPath, ChildrenDefinition>? children = null,
        IEnumerable<FromDerivatives>? fromDerivatives = null) =>
        new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "test-projection",
            "test-read-model",
            true,
            true,
            new System.Text.Json.Nodes.JsonObject(),
            from ?? new Dictionary<EventType, FromDefinition>(),
            join ?? new Dictionary<EventType, JoinDefinition>(),
            children ?? new Dictionary<PropertyPath, ChildrenDefinition>(),
            fromDerivatives ?? [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            null,
            DateTimeOffset.UtcNow,
            null,
            AutoMap.Enabled);
}
