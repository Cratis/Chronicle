// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[Collection(ChronicleCollection.Name)]
public class when_appending_an_event_with_reactor_reducer_and_projection : IClassFixture<ClusteringFixture>, IAsyncLifetime
{
    readonly ClusteringFixture _fixture;
    readonly TaskCompletionSource _reactorHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _reducerHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    EventSourceId _eventSourceId = string.Empty;
    ClusteredReducerReadModel? _reducerReadModel;

    public ClusteredReactor Reactor { get; private set; } = default!;
    public ClusteredReducer Reducer { get; private set; } = default!;
    public ReactorState ReactorState { get; private set; } = default!;
    public ReducerState ReducerState { get; private set; } = default!;
    public ProjectionState ProjectionState { get; private set; } = default!;

    public when_appending_an_event_with_reactor_reducer_and_projection(ClusteringFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.RemoveAllDatabases();
        _eventSourceId = "clustered-source";

        // Execute the scenario
        var startupTimeout = TimeSpanFactory.FromSeconds(30);
        var eventStore = _fixture.EventStore;
        
        var reactorHandler = eventStore.Reactors.GetHandlerFor<ClusteredReactor>();
        var reducerHandler = eventStore.Reducers.GetHandlerFor<ClusteredReducer>();
        var projectionHandler = eventStore.Projections.GetHandlerFor<ClusteredProjection>();

        await reactorHandler.WaitTillActive(startupTimeout);
        await reducerHandler.WaitTillActive(startupTimeout);
        await projectionHandler.WaitTillActive(startupTimeout);

        var appendResult = await eventStore.EventLog.Append(_eventSourceId, new ClusteredEvent(42));

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

    public Task DisposeAsync() => Task.CompletedTask;

    public ClusteredReducerReadModel ReducerReadModel => _reducerReadModel!;

    async Task<TReadModel?> GetReadModel<TReadModel>(ReadModelContainerName containerName, string key)
        where TReadModel : class
    {
        var filter = Builders<TReadModel>.Filter.Eq("_id", key);
        var cursor = await _fixture.ReadModels.Database
            .GetCollection<TReadModel>(containerName)
            .FindAsync(filter);
        return await cursor.FirstOrDefaultAsync();
    }

    [Fact]
    public void should_have_reactor_called() => Reactor.HandledEvents.ShouldEqual(1);

    [Fact]
    public void should_have_reducer_called_and_produce_desired_result()
    {
        Reducer.HandledEvents.ShouldEqual(1);
        ReducerReadModel.Number.ShouldEqual(42);
    }

    [Fact]
    public void should_have_projection_called_and_produce_desired_result() => 
        ProjectionState.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.First);

    [Fact]
    public void should_have_reactor_active() => ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    public void should_have_reducer_active() => ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    public void should_have_projection_active() => ProjectionState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [EventType]
    public record ClusteredEvent(int Number);

    public class ClusteredReactor : IReactor
    {
        int _handledEvents;
        public int HandledEvents => _handledEvents;

        public Task OnClusteredEvent(ClusteredEvent @event, EventContext context)
        {
            Interlocked.Increment(ref _handledEvents);
            return Task.CompletedTask;
        }
    }

    public record ClusteredReducerReadModel(int Number);

    public class ClusteredReducer : IReducerFor<ClusteredReducerReadModel>
    {
        int _handledEvents;
        public int HandledEvents => _handledEvents;

        public Task<ClusteredReducerReadModel?> OnClusteredEvent(ClusteredEvent @event, ClusteredReducerReadModel? current, EventContext context)
        {
            Interlocked.Increment(ref _handledEvents);
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
