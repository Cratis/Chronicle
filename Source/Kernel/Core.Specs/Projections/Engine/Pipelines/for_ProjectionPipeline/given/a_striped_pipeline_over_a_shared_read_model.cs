// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.given;

public class a_striped_pipeline_over_a_shared_read_model : Specification
{
    protected ProjectionPipeline _pipeline;
    protected IProjection _projection;
    protected EventType _eventType;
    protected ConcurrentDictionary<string, PartitionCounters> _counters;

    void Establish()
    {
        _eventType = new EventType("d6e1f4c0-6b2a-4a0e-9d47-2a8f5c0e1b33", EventTypeGeneration.First);
        _counters = new ConcurrentDictionary<string, PartitionCounters>();

        _projection = Substitute.For<IProjection>();
        _projection.IsEventSourceKeyed.Returns(true);
        _projection.GetOperationTypeFor(Arg.Any<EventType>()).Returns(ProjectionOperationType.None);

        _pipeline = new ProjectionPipeline(
            _projection,
            Substitute.For<ISink>(),
            Substitute.For<IChangesetStorage>(),
            Substitute.For<IObjectComparer>(),
            [new ReadModifyWriteStep(_counters)],
            new ProjectionHandleLock(),
            Substitute.For<IReplayScopedCache>(),
            NullLogger<ProjectionPipeline>.Instance);
    }

    protected AppendedEvent EventFor(string eventSourceId) =>
        new(EventContext.Empty with { EventSourceId = eventSourceId, EventType = _eventType }, new ExpandoObject());

    protected sealed class PartitionCounters
    {
        public int ReadModelValue;
        public int Current;
        public int Max;
    }

    /// <summary>
    /// A pipeline step that performs a deliberately non-atomic read-modify-write against a per-partition counter, with
    /// a delay between the read and the write to widen the interleaving window. Correct striping serializes handling
    /// for the same event source id, so no update is ever lost and the same partition is never handled twice at once;
    /// a broken lock would interleave the read and the write and drop increments.
    /// </summary>
    /// <param name="counters">Per-partition counters keyed by event source id.</param>
    protected sealed class ReadModifyWriteStep(ConcurrentDictionary<string, PartitionCounters> counters) : ICanPerformProjectionPipelineStep
    {
        public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
        {
            var partition = counters.GetOrAdd(context.Event.Context.EventSourceId.Value, static _ => new PartitionCounters());

            var current = Interlocked.Increment(ref partition.Current);
            RecordMax(ref partition.Max, current);

            var value = partition.ReadModelValue;
            await Task.Delay(1);
            partition.ReadModelValue = value + 1;

            Interlocked.Decrement(ref partition.Current);

            return context;
        }

        static void RecordMax(ref int max, int value)
        {
            int current;
            do
            {
                current = Volatile.Read(ref max);
                if (value <= current)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref max, value, current) != current);
        }
    }
}
