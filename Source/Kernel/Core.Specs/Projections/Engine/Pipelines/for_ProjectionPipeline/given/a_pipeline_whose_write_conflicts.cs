// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.given;

public class a_pipeline_whose_write_conflicts : Specification
{
    protected ProjectionPipeline _pipeline;
    protected ConflictingStep _step;

    protected virtual bool SupportsFencedGlobalWrites => true;

    protected virtual int ConflictsBeforeSucceeding => 1;

    void Establish()
    {
        var projection = Substitute.For<IProjection>();
        projection.SupportsFencedGlobalWrites.Returns(SupportsFencedGlobalWrites);
        projection.IsEventSourceKeyed.Returns(true);
        projection.ReadModel.Returns(new ReadModelDefinition(
            "test-read-model",
            "TestReadModel",
            "TestReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []));
        projection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(ProjectionOperationType.None);

        _step = new ConflictingStep(ConflictsBeforeSucceeding);
        _pipeline = new ProjectionPipeline(
            projection,
            Substitute.For<ISink>(),
            Substitute.For<IChangesetStorage>(),
            Substitute.For<IObjectComparer>(),
            [_step],
            new ProjectionHandleLock(),
            Substitute.For<IReplayScopedCache>(),
            NullLogger<ProjectionPipeline>.Instance,
            null,
            false);
    }

    protected static AppendedEvent SomeEvent() =>
        new(EventContext.Empty with { EventSourceId = "source", EventType = new EventType("event", EventTypeGeneration.First) }, new ExpandoObject());

    /// <summary>
    /// A step standing in for the whole read-modify-write: it builds a fresh changeset on every attempt and reports a conflict as the sink does.
    /// </summary>
    /// <param name="conflictsBeforeSucceeding">How many attempts conflict before one succeeds.</param>
    protected sealed class ConflictingStep(int conflictsBeforeSucceeding) : ICanPerformProjectionPipelineStep
    {
        public List<IChangeset<AppendedEvent, ExpandoObject>> Changesets { get; } = [];

        public List<ProjectionEventContext> Contexts { get; } = [];

        public ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
        {
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            Changesets.Add(changeset);
            Contexts.Add(context);
            if (Changesets.Count <= conflictsBeforeSucceeding)
            {
                throw new SinkWriteConflictException(new Key("source", ArrayIndexers.NoIndexers));
            }

            return ValueTask.FromResult(context with { Changeset = changeset });
        }
    }
}
