// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Storage.Changes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_running_pipelines;

/// <summary>
/// Runs the real projection pipeline steps that read initial state and save changes against the event sequence sink.
/// </summary>
public class and_a_projection_publishes_over_several_events : given.a_sink
{
    ProjectionPipeline _pipeline;
    ProjectionEventContext _first;
    ProjectionEventContext _second;

    void Establish()
    {
        var projection = Substitute.For<IProjection>();
        projection.IsEventSourceKeyed.Returns(true);
        projection.ChildProjections.Returns([]);
        projection.ReadModel.Returns(_definition);
        projection.TargetReadModelSchema.Returns(_definition.GetTargetSchema());
        projection.InitialModelState.Returns(new ExpandoObject());
        projection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(ProjectionOperationType.From);

        _pipeline = new ProjectionPipeline(
            projection,
            _sink,
            Substitute.For<IChangesetStorage>(),
            new ObjectComparer(),
            [
                new Resolve(),
                new SetInitialState(_sink, NullLogger<SetInitialState>.Instance),
                new Accumulate(),
                new SaveChanges(_sink, Substitute.For<IChangesetStorage>(), true, NullLogger<SaveChanges>.Instance)
            ],
            new ProjectionHandleLock(),
            Substitute.For<IReplayScopedCache>(),
            NullLogger<ProjectionPipeline>.Instance);
    }

    async Task Because()
    {
        _first = await _pipeline.Handle(Event(5));
        _second = await _pipeline.Handle(Event(9));
    }

    [Fact] void should_not_fail_any_partition() => (_first.FailedPartitions.Any() || _second.FailedPartitions.Any()).ShouldBeFalse();
    [Fact] void should_publish_a_new_instance_per_event() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(2);
    [Fact] void should_accumulate_through_the_published_state() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Select(_ => ((IDictionary<string, object?>)_.Content)["Count"]?.ToString()).ShouldContainOnly("1", "2");

    [Fact] void should_not_publish_sink_bookkeeping() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).SelectMany(_ => ((IDictionary<string, object?>)_.Content).Keys).Where(_ => _.StartsWith("__", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_only_publish_declared_properties() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).SelectMany(_ => ((IDictionary<string, object?>)_.Content).Keys).Distinct().Order().ShouldContainOnly("Count", "Total");

    static AppendedEvent Event(ulong number) => new(
        EventContext.From("store", "tenant", new("Source", EventTypeGeneration.First), EventSourceType.Default, "key", EventStreamType.All, EventStreamId.Default, number, CorrelationId.New(), null, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
        new ExpandoObject());

    sealed class Resolve : ICanPerformProjectionPipelineStep
    {
        public ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context) =>
            ValueTask.FromResult(context with { Key = new Key("key", Properties.ArrayIndexers.NoIndexers) });
    }

    sealed class Accumulate : ICanPerformProjectionPipelineStep
    {
        public ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
        {
            var initial = (IDictionary<string, object?>)context.Changeset.InitialState;
            var count = initial.TryGetValue("Count", out var existing) && existing is not null ? Convert.ToInt64(existing) : 0;
            var next = new ExpandoObject();
            ((IDictionary<string, object?>)next)["Total"] = (count + 1) * 10;
            ((IDictionary<string, object?>)next)["Count"] = count + 1;
            new ObjectComparer().Compare(context.Changeset.InitialState, next, out var differences);
            ((Changeset<AppendedEvent, ExpandoObject>)context.Changeset).ReplaceState(next, differences);
            return ValueTask.FromResult(context);
        }
    }
}
