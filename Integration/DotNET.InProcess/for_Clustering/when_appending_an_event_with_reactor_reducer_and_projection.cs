// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.for_Clustering.when_appending_an_event_with_reactor_reducer_and_projection.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Clustering;

[Collection(ChronicleCollection.Name)]
public class when_appending_an_event_with_reactor_reducer_and_projection(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        readonly TaskCompletionSource _reactorHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _reducerHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        EventSourceId _eventSourceId = string.Empty;
        ClusteredReducerReadModel? _reducerReadModel;

        public ClusteredReactor Reactor { get; private set; } = default!;
        public ClusteredReducer Reducer { get; private set; } = default!;
        public ReactorState ReactorState { get; private set; } = default!;
        public ReducerState ReducerState { get; private set; } = default!;
        public ProjectionState ProjectionState { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(ClusteredEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ClusteredReactor)];
        public override IEnumerable<Type> Reducers => [typeof(ClusteredReducer)];
        public override IEnumerable<Type> Projections => [typeof(ClusteredProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new(_reactorHandled);
            Reducer = new(_reducerHandled);

            services.AddSingleton(Reactor);
            services.AddSingleton(Reducer);
        }

        void Establish() => _eventSourceId = "clustered-source";

        async Task Because()
        {
            var startupTimeout = TimeSpanFactory.FromSeconds(30);
            var reactorHandler = EventStore.Reactors.GetHandlerFor<ClusteredReactor>();
            var reducerHandler = EventStore.Reducers.GetHandlerFor<ClusteredReducer>();
            var projectionHandler = EventStore.Projections.GetHandlerFor<ClusteredProjection>();

            await reactorHandler.WaitTillActive(startupTimeout);
            await reducerHandler.WaitTillActive(startupTimeout);
            await projectionHandler.WaitTillActive(startupTimeout);

            var appendResult = await EventStore.EventLog.Append(_eventSourceId, new ClusteredEvent(42));

            await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, startupTimeout);
            await reducerHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, startupTimeout);
            await projectionHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, startupTimeout);

            await _reactorHandled.Task.WaitAsync(startupTimeout);
            await _reducerHandled.Task.WaitAsync(startupTimeout);

            _reducerReadModel = await GetReadModel<ClusteredReducerReadModel>(reducerHandler.ContainerName, _eventSourceId);
            ReactorState = await reactorHandler.GetState();
            ReducerState = await reducerHandler.GetState();
            ProjectionState = await projectionHandler.GetState();
        }

        public ClusteredReducerReadModel ReducerReadModel => _reducerReadModel!;

        async Task<TReadModel?> GetReadModel<TReadModel>(ReadModelContainerName containerName, string key)
            where TReadModel : class
        {
            var filter = Builders<TReadModel>.Filter.Eq("_id", key);
            var cursor = await ChronicleFixture.ReadModels.Database
                .GetCollection<TReadModel>(containerName)
                .FindAsync(filter);
            return await cursor.FirstOrDefaultAsync();
        }
    }

    [Fact]
    void should_have_reactor_called() => Context.Reactor.HandledEvents.ShouldEqual(1);

    [Fact]
    void should_have_reducer_called_and_produce_desired_result()
    {
        Context.Reducer.HandledEvents.ShouldEqual(1);
        Context.ReducerReadModel.Number.ShouldEqual(42);
    }

    [Fact]
    void should_have_projection_called_and_produce_desired_result() => Context.ProjectionState.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);

    [Fact]
    void should_have_reactor_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_reducer_active() => Context.ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_projection_active() => Context.ProjectionState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [EventType]
    public record ClusteredEvent(int Number);

    public class ClusteredReactor(TaskCompletionSource handled) : IReactor
    {
        int _handledEvents;
        public int HandledEvents => _handledEvents;

        public Task OnClusteredEvent(ClusteredEvent @event, EventContext context)
        {
            Interlocked.Increment(ref _handledEvents);
            handled.TrySetResult();
            return Task.CompletedTask;
        }
    }

    public record ClusteredReducerReadModel(int Number);

    public class ClusteredReducer(TaskCompletionSource handled) : IReducerFor<ClusteredReducerReadModel>
    {
        int _handledEvents;
        public int HandledEvents => _handledEvents;

        public Task<ClusteredReducerReadModel?> OnClusteredEvent(ClusteredEvent @event, ClusteredReducerReadModel? current, EventContext context)
        {
            Interlocked.Increment(ref _handledEvents);
            handled.TrySetResult();
            return Task.FromResult<ClusteredReducerReadModel?>(new(@event.Number));
        }
    }

    public record ClusteredProjectionReadModel(int Number);

    public class ClusteredProjection : IProjectionFor<ClusteredProjectionReadModel>
    {
        public ProjectionId Identifier => "clustered-projection";

        public void Define(IProjectionBuilderFor<ClusteredProjectionReadModel> builder) => builder
            .From<ClusteredEvent>(events => events
                .Set(model => model.Number)
                .To(@event => @event.Number));
    }
}
