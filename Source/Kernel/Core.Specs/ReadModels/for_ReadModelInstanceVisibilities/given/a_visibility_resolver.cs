// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine;
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
    protected IProjection _projection;
    protected ReadModelInstanceVisibilities _resolver;
    protected ReadModelInstanceVisibility _result;

    void Establish()
    {
        _projectionsManager = Substitute.For<EngineProjectionsManager>();
        _projection = Substitute.For<IProjection>();
        _projection.HasParent.Returns(false);
        _projection.SubscribesToAllEvents.Returns(false);
        _projection.ChildProjections.Returns([Substitute.For<IProjection>()]);
        _projection.OperationTypes.Returns(new Dictionary<EventType, ProjectionOperationType>());
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

    protected void ProjectionIsRegistered(IProjection projection) =>
        _projectionsManager.TryGet(EventStore, Namespace, ProjectionId, out Arg.Any<IProjection?>()).Returns(call =>
        {
            call[3] = projection;
            return true;
        });
}
