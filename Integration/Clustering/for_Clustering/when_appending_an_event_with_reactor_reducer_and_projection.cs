// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.Clustering.for_Clustering.when_appending_an_event_with_reactor_reducer_and_projection.context;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[Collection(ChronicleCollection.Name)]
public class when_appending_an_event_with_reactor_reducer_and_projection(context _context)
    : IClassFixture<context>
{
    public class context(ClusteringFixture fixture) : IAsyncLifetime
    {
        readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);

        ClusteredEvent _expected = default!;
        ClusteredReducerReadModel _reducerReadModel = default!;
        ClusteredProjectionReadModel _projectionReadModel = default!;
        ReactorState _reactorState = default!;
        ReducerState _reducerState = default!;
        ProjectionState _projectionState = default!;

        public ClusteredEvent Expected => _expected;
        public ClusteredReducerReadModel ReducerReadModel => _reducerReadModel;
        public ClusteredProjectionReadModel ProjectionReadModel => _projectionReadModel;
        public ReactorState ReactorState => _reactorState;
        public ReducerState ReducerState => _reducerState;
        public ProjectionState ProjectionState => _projectionState;
        public ClusteredReactorSignal ReactorSignal { get; private set; } = default!;
        public bool IsEventSequenceGrainOnEventSequencesSilo { get; private set; }
        public bool IsObserverGrainOnObserversSilo { get; private set; }

        public async Task InitializeAsync()
        {
            var eventStore = fixture.ClientEventStore;
            ReactorSignal = fixture.ReactorSignal;
            ReactorSignal.Reset();

            var reactorHandler = eventStore.Reactors.GetHandlerFor<ClusteredReactor>();
            var reducerHandler = eventStore.Reducers.GetHandlerFor<ClusteredReducer>();
            var projectionHandler = eventStore.Projections.GetHandlerFor<ClusteredProjection>();

            _expected = new ClusteredEvent(
                Number: 42,
                Reference: ThingId.New(),
                Priority: Priority.High,
                Tags: ["alpha", "beta", "gamma"],
                Location: new Address("221B Baker Street", "London", 11111));

            const string eventSourceId = "clustered-source";

            await reactorHandler.WaitTillActive(_timeout);
            await reducerHandler.WaitTillActive(_timeout);
            await projectionHandler.WaitTillActive(_timeout);

            var appendResult = await eventStore.EventLog.Append(eventSourceId, _expected);

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await reducerHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
            await projectionHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);

            await ReactorSignal.Handled.WaitAsync(_timeout);

            _reducerReadModel = await eventStore.ReadModels.GetInstanceById<ClusteredReducerReadModel>(eventSourceId);
            _projectionReadModel = await eventStore.ReadModels.GetInstanceById<ClusteredProjectionReadModel>(eventSourceId);
            _reactorState = await reactorHandler.GetState();
            _reducerState = await reducerHandler.GetState();
            _projectionState = await projectionHandler.GetState();

            var management = fixture.SiloServices.GetRequiredService<IGrainFactory>().GetGrain<IManagementGrain>(0);
            var grainStats = await management.GetDetailedGrainStatistics();
            var eventSequencesSiloAddress = fixture.EventSequencesSiloAddress;
            var observersSiloAddress = fixture.ObserversSiloAddress;

            IsEventSequenceGrainOnEventSequencesSilo = grainStats.Any(s =>
                s.SiloAddress == eventSequencesSiloAddress &&
                s.GrainType.Contains("eventsequence", StringComparison.OrdinalIgnoreCase));

            IsObserverGrainOnObserversSilo = grainStats.Any(s =>
            {
                var grainType = s.GrainType;
                return s.SiloAddress == observersSiloAddress &&
                       grainType.Contains("observer", StringComparison.OrdinalIgnoreCase) &&
                       !grainType.Contains("subscriber", StringComparison.OrdinalIgnoreCase);
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [Fact] void should_have_reactor_called() => _context.ReactorSignal.HandledCount.ShouldEqual(1);
    [Fact] void should_have_reactor_observe_the_rich_event() => _context.ReactorSignal.LastHandledReference.ShouldEqual(_context.Expected.Reference.Value);
    [Fact] void should_have_reducer_round_trip_the_number() => _context.ReducerReadModel.Number.ShouldEqual(42);
    [Fact] void should_have_reducer_round_trip_the_concept() => _context.ReducerReadModel.Reference.ShouldEqual(_context.Expected.Reference);
    [Fact] void should_have_reducer_round_trip_the_enum() => _context.ReducerReadModel.Priority.ShouldEqual(Priority.High);
    [Fact] void should_have_reducer_round_trip_the_collection() => _context.ReducerReadModel.Tags.ShouldContainOnly(_context.Expected.Tags);
    [Fact] void should_have_reducer_round_trip_the_nested_record() => _context.ReducerReadModel.Location.City.ShouldEqual("London");
    [Fact] void should_have_projection_round_trip_the_number() => _context.ProjectionReadModel.Number.ShouldEqual(42);
    [Fact] void should_have_reactor_active() => _context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_reducer_active() => _context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_projection_active() => _context.ProjectionState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_event_sequence_grain_on_event_sequences_silo() => _context.IsEventSequenceGrainOnEventSequencesSilo.ShouldBeTrue();
    [Fact] void should_have_observer_grain_on_observers_silo() => _context.IsObserverGrainOnObserversSilo.ShouldBeTrue();
}
