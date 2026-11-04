// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching_a_page_to_a_batch_reactor.given;

public class a_batch_reactor_dispatcher : for_ReactorDispatcher.given.all_dependencies
{
    internal ReactorDispatcher _dispatcher;
    protected ReactorHandler _handler;
    protected Recorder _recorder;

    protected PartitionResult First => _published[0].Partitions.Single(_ => _.Partition == "first");
    protected PartitionResult Second => _published[0].Partitions.Single(_ => _.Partition == "second");

    void Establish()
    {
        _recorder = new();
        _argumentsResolver = new ReactorMethodArgumentsResolver();
        _eventTypes.AllClrTypes.Returns(new[] { typeof(Registered), typeof(Removed) }.ToImmutableList());
        _eventTypes.GetEventTypeFor(typeof(Registered)).Returns(new EventType("registered", 1));
        _eventTypes.GetEventTypeFor(typeof(Removed)).Returns(new EventType("removed", 1));
        _eventTypes.GetClrTypeFor(new EventTypeId("registered"), Arg.Any<EventTypeGeneration>()).Returns(typeof(Registered));
        _eventTypes.GetClrTypeFor(new EventTypeId("removed"), Arg.Any<EventTypeGeneration>()).Returns(typeof(Removed));
        _eventSerializer.Deserialize(Arg.Any<Type>(), Arg.Any<JsonObject>()).Returns(call => Task.FromResult(Activator.CreateInstance(call.Arg<Type>())!));
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(BatchReactor)).Returns(_ =>
            new ActivatedArtifact(new BatchReactor(_recorder), typeof(BatchReactor), Substitute.For<ILogger<ActivatedArtifact>>()));
        _dispatcher = new(
            _eventStore,
            _eventTypes,
            _serviceProvider,
            _artifactActivator,
            _middlewaresActivator,
            _eventSerializer,
            _activitySource,
            _sideEffectHandlers,
            _reactorContextValuesBuilder,
            _argumentsResolver,
            _logger,
            LoggerFactory);
        _handler = new(_eventStore, "batch-reactor", typeof(BatchReactor), EventSequenceId.Log, [new("registered", 1), new("removed", 1)], _causationManager, _identityProvider);
    }

    void Destroy() => _handler.Dispose();

    protected Task Dispatch(EventsToObserve events) => _dispatcher.Dispatch(_handler, null, events, "ns", Publish, CancellationToken.None);

    protected static EventsToObserve Page(params (string Partition, (ulong SequenceNumber, string EventType)[] Events)[] partitions) => new()
    {
        DeliveryId = 42,
        Origin = Contracts.Observation.ObservationOrigin.Replaying,
        Partitions = [.. partitions.Select(partition => new PartitionEvents
        {
            Partition = partition.Partition,
            Events = [.. partition.Events.Select(@event => new Contracts.Events.AppendedEvent
            {
                Context = (EventContext.Empty with
                {
                    EventType = new(@event.EventType, 1),
                    SequenceNumber = @event.SequenceNumber,
                    EventSourceId = partition.Partition
                }).ToContract(),
                Content = "{}",
                GenerationalContent = new Dictionary<int, string>()
            })]
        })]
    };

    public record Registered;

    public record Removed;

    public class Recorder
    {
        public List<ulong> Batched { get; } = [];
        public List<ulong> PerEvent { get; } = [];
        public BatchContext? Context { get; set; }
        public Action<EventBatch> OnBatch { get; set; } = _ => { };
    }

    public class BatchReactor(Recorder recorder) : IReactor
    {
        [Observes<Registered>]
        public Task Index(EventBatch batch, BatchContext context)
        {
            recorder.Batched.AddRange(batch.Select(_ => _.Context.SequenceNumber.Value));
            recorder.Context = context;
            recorder.OnBatch(batch);
            return Task.CompletedTask;
        }

        public Task Removed(Removed @event, EventContext context)
        {
            recorder.PerEvent.Add(context.SequenceNumber.Value);
            return Task.CompletedTask;
        }
    }
}
