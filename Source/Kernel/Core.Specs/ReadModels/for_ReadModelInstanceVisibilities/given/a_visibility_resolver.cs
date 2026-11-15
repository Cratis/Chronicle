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
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using EngineProjectionsManager = Cratis.Chronicle.Projections.Engine.IProjectionsManager;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceVisibilities.given;

public class a_visibility_resolver : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName Namespace = "test-namespace";
    protected static readonly ProjectionId ProjectionId = "the-projection";

    protected EngineProjectionsManager _projectionsManager;
    protected ProjectionDefinition _projection;
    protected ReadModelInstanceVisibilities _resolver;
    protected ReadModelInstanceVisibility _result;

    void Establish()
    {
        _projectionsManager = Substitute.For<EngineProjectionsManager>();
        _projection = new(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            ProjectionId,
            "model",
            true,
            true,
            new(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["children"] = new("id", new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>())
            },
            [],
            new(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            null);
        _resolver = new(_projectionsManager);
    }

    protected static ReadModelDefinition DefinitionObservedBy(ReadModelObserverType observerType) => new(
        "model",
        "container",
        "Model",
        ReadModelOwner.None,
        ReadModelSource.Unknown,
        observerType,
        ProjectionId.Value,
        new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory),
        new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
        []);

    protected void ProjectionIsRegistered(ProjectionDefinition projection) =>
        _projectionsManager.GetDefinition(EventStore, ProjectionId).Returns(projection);
}
