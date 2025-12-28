// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

public class a_projections_manager : Specification
{
    protected ProjectionsManager _manager;
    protected IProjectionFactory _projectionFactory;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;
    protected ProjectionDefinition _firstDefinition;
    protected ProjectionDefinition _secondDefinition;
    protected ReadModelDefinition _firstReadModelDefinition;
    protected ReadModelDefinition _secondReadModelDefinition;

    void Establish()
    {
        _projectionFactory = Substitute.For<IProjectionFactory>();
        _projectionFactory.Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>())
            .Returns(callInfo => Substitute.For<IProjection>());

        _manager = new ProjectionsManager(_projectionFactory);
        _eventStore = "event-store";
        _namespace = "namespace";

        _firstDefinition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "first-projection",
            "first-read-model",
            true,
            true,
            new System.Text.Json.Nodes.JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            SinkDefinition.None,
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            null,
            DateTimeOffset.UtcNow);

        _secondDefinition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "second-projection",
            "second-read-model",
            true,
            true,
            new System.Text.Json.Nodes.JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            SinkDefinition.None,
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            null,
            DateTimeOffset.UtcNow);

        _firstReadModelDefinition = new ReadModelDefinition(
            "first-read-model",
            "FirstReadModel",
            ReadModelOwner.Client,
            SinkTypeId.None,
            SinkConfigurationId.None,
            new Dictionary<ReadModelGeneration, NJsonSchema.JsonSchema>(),
            []);

        _secondReadModelDefinition = new ReadModelDefinition(
            "second-read-model",
            "SecondReadModel",
            ReadModelOwner.Client,
            SinkTypeId.None,
            SinkConfigurationId.None,
            new Dictionary<ReadModelGeneration, NJsonSchema.JsonSchema>(),
            []);
    }
}
