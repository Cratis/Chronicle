// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_generation;

public class a_generation_pipeline_manager : for_ProjectionPipelineManager.given.a_pipeline_manager
{
    protected MaterializationGenerationIdentity _second;
    protected ISink _generationSink;

    void Establish()
    {
        _projection.Scope.Returns(ProjectionScope.Namespaced);
        _projection.ReadModel.Returns(new ReadModelDefinition("model", "Model", "Model", ReadModelOwner.None, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), []));
        _second = MaterializationGenerationIdentity.For("projection", 2U, "Model");
        _generationSink = Substitute.For<ISink>();
        _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.GetFor(_projection.ReadModel).Returns(_sink);
        _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.GetFor(_projection.ReadModel, _second.Destination).Returns(_generationSink);
    }

    protected static T Field<T>(IProjectionPipeline pipeline, string name) =>
        (T)typeof(ProjectionPipeline).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pipeline)!;

    protected static object HandleLock(IProjectionPipeline pipeline) => Field<object>(pipeline, "_handleLock");
}
