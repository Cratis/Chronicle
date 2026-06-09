// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[Collection(ChronicleCollection.Name)]
public class when_appending_an_event_with_reactor_reducer_and_projection(ClusteringFixture fixture) : IAsyncLifetime
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
    static Task<Outcome>? _work;

    Outcome _outcome = default!;

    ClusteredEvent _expected => _outcome.Expected;
    ClusteredReducerReadModel _reducerReadModel => _outcome.ReducerReadModel;
    ClusteredProjectionReadModel _projectionReadModel => _outcome.ProjectionReadModel;

    public ReactorState ReactorState => _outcome.ReactorState;
    public ReducerState ReducerState => _outcome.ReducerState;
    public ProjectionState ProjectionState => _outcome.ProjectionState;

    public async Task InitializeAsync() => _outcome = await (_work ??= Load(fixture));

    public Task DisposeAsync() => Task.CompletedTask;

    static async Task<Outcome> Load(ClusteringFixture fixture)
    {
        ClusteredReactor.Reset();

        var eventStore = fixture.ClientEventStore;

        var reactorHandler = eventStore.Reactors.GetHandlerFor<ClusteredReactor>();
        var reducerHandler = eventStore.Reducers.GetHandlerFor<ClusteredReducer>();
        var projectionHandler = eventStore.Projections.GetHandlerFor<ClusteredProjection>();

        // A rich payload — concept, enum, collection and nested record — to verify these all survive the
        // Orleans round-trip as the event and read models cross silo boundaries between the event sequence,
        // the observers and the connected client. The same instance is reused across retries (fixed
        // Reference) so the read-model and reactor assertions hold regardless of which attempt succeeds.
        var expected = new ClusteredEvent(
            Number: 42,
            Reference: ThingId.New(),
            Priority: Priority.High,
            Tags: ["alpha", "beta", "gamma"],
            Location: new Address("221B Baker Street", "London", 11111));

        const string eventSourceId = "clustered-source";

        await reactorHandler.WaitTillActive(_timeout);
        await reducerHandler.WaitTillActive(_timeout);
        await projectionHandler.WaitTillActive(_timeout);

        var appendResult = await eventStore.EventLog.Append(eventSourceId, expected);

        await reactorHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
        await reducerHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);
        await projectionHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber, _timeout);

        await ClusteredReactor.Handled.Task.WaitAsync(_timeout);

        return new Outcome(
            expected,
            await eventStore.ReadModels.GetInstanceById<ClusteredReducerReadModel>(eventSourceId),
            await eventStore.ReadModels.GetInstanceById<ClusteredProjectionReadModel>(eventSourceId),
            await reactorHandler.GetState(),
            await reducerHandler.GetState(),
            await projectionHandler.GetState());
    }

    record Outcome(
        ClusteredEvent Expected,
        ClusteredReducerReadModel ReducerReadModel,
        ClusteredProjectionReadModel ProjectionReadModel,
        ReactorState ReactorState,
        ReducerState ReducerState,
        ProjectionState ProjectionState);

    [Fact] void should_have_reactor_called() => ClusteredReactor.HandledCount.ShouldEqual(1);
    [Fact] void should_have_reactor_observe_the_rich_event() => ClusteredReactor.LastReference.ShouldEqual(_expected.Reference);
    [Fact] void should_have_reducer_round_trip_the_number() => _reducerReadModel.Number.ShouldEqual(42);
    [Fact] void should_have_reducer_round_trip_the_concept() => _reducerReadModel.Reference.ShouldEqual(_expected.Reference);
    [Fact] void should_have_reducer_round_trip_the_enum() => _reducerReadModel.Priority.ShouldEqual(Priority.High);
    [Fact] void should_have_reducer_round_trip_the_collection() => _reducerReadModel.Tags.ShouldContainOnly(_expected.Tags);
    [Fact] void should_have_reducer_round_trip_the_nested_record() => _reducerReadModel.Location.City.ShouldEqual("London");
    [Fact] void should_have_projection_round_trip_the_number() => _projectionReadModel.Number.ShouldEqual(42);
    [Fact] void should_have_reactor_active() => ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_reducer_active() => ReducerState.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_have_projection_active() => ProjectionState.RunningState.ShouldEqual(ObserverRunningState.Active);

    public record ThingId(Guid Value) : ConceptAs<Guid>(Value)
    {
        public static ThingId New() => new(Guid.NewGuid());
    }

    public enum Priority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public record Address(string Street, string City, int ZipCode);

    [EventType]
    public record ClusteredEvent(int Number, ThingId Reference, Priority Priority, IList<string> Tags, Address Location);

    public class ClusteredReactor : IReactor
    {
        static int _handledCount;

        public static int HandledCount => _handledCount;

        public static ThingId LastReference { get; private set; } = default!;

        public static TaskCompletionSource Handled { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static void Reset()
        {
            Interlocked.Exchange(ref _handledCount, 0);
            LastReference = default!;
            Handled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task OnClusteredEvent(ClusteredEvent @event, EventContext context)
        {
            LastReference = @event.Reference;
            Interlocked.Increment(ref _handledCount);
            Handled.TrySetResult();
            return Task.CompletedTask;
        }
    }

    public record ClusteredReducerReadModel(int Number, ThingId Reference, Priority Priority, IList<string> Tags, Address Location);

    public class ClusteredReducer : IReducerFor<ClusteredReducerReadModel>
    {
        public Task<ClusteredReducerReadModel?> OnClusteredEvent(ClusteredEvent @event, ClusteredReducerReadModel? current, EventContext context) =>
            Task.FromResult<ClusteredReducerReadModel?>(new(@event.Number, @event.Reference, @event.Priority, @event.Tags, @event.Location));
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
