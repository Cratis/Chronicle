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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline;

public class when_handling_an_event_without_a_bulk_window : Specification
{
    ISink _sink;
    ProjectionPipeline _pipeline;

    void Establish()
    {
        _sink = Substitute.For<ISink>();
        var projection = Substitute.For<IProjection>();
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

        _pipeline = new ProjectionPipeline(
            projection,
            _sink,
            Substitute.For<IChangesetStorage>(),
            Substitute.For<IObjectComparer>(),
            [new ChangingStep(), new SaveChanges(_sink, Substitute.For<IChangesetStorage>(), false, Substitute.For<ILogger<SaveChanges>>())],
            new ProjectionHandleLock(),
            Substitute.For<IReplayScopedCache>(),
            NullLogger<ProjectionPipeline>.Instance,
            null,
            false);
    }

    async Task Because()
    {
        await _pipeline.BeginBulk();
        await _pipeline.ResumeBulk();
        await _pipeline.Handle(new AppendedEvent(EventContext.Empty with { EventSourceId = "source", EventType = new EventType("event", EventTypeGeneration.First) }, new ExpandoObject()));
        await _pipeline.FlushBulk();
        await _pipeline.EndBulk();
        await _pipeline.EndBulk(CancellationToken.None);
    }

    [Fact] void should_write_the_event_to_the_sink() => _sink.Received(1).ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>());
    [Fact] void should_not_begin_a_bulk_window() => _sink.DidNotReceive().BeginBulk();
    [Fact] void should_not_resume_a_bulk_window() => _sink.DidNotReceive().ResumeBulk();
    [Fact] void should_not_flush_a_bulk_window() => _sink.DidNotReceive().FlushBulk();
    [Fact] void should_not_end_a_bulk_window() => _sink.DidNotReceive().EndBulk();
    [Fact] void should_not_end_a_bulk_window_with_cancellation() => _sink.DidNotReceive().EndBulk(Arg.Any<CancellationToken>());

    sealed class ChangingStep : ICanPerformProjectionPipelineStep
    {
        public ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
        {
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.HasChanges.Returns(true);
            changeset.Changes.Returns([]);
            changeset.CurrentState.Returns(new ExpandoObject());
            return ValueTask.FromResult(context with { Key = new Key("source", ArrayIndexers.NoIndexers), Changeset = changeset });
        }
    }
}
