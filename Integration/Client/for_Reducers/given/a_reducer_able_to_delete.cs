// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers.given;

public class a_reducer_able_to_delete<TReducer>(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    where TReducer : class, IReducerFor<SomeReadModel>, new()
{
    public TReducer Reducer;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(SomeDeleteEvent)];
    public override IEnumerable<Type> Reducers => [typeof(TReducer)];

    public long CountAfterFirstEvent;
    public long CountAfterSecondEvent;

    protected IReducerHandler _reducer;

    long _netCount;
#pragma warning disable CA2213
    IDisposable _subscription;
#pragma warning restore CA2213

    async Task Establish()
    {
        _subscription = EventStore.ReadModels.Watch<SomeReadModel>()
            .Subscribe(cs =>
            {
                if (cs.Removed)
                {
                    Interlocked.Decrement(ref _netCount);
                }
                else
                {
                    Interlocked.Increment(ref _netCount);
                }
            });

        var startupTimeout = TimeSpanFactory.FromSeconds(30);
        _reducer = EventStore.Reducers.GetHandlerFor<TReducer>();
        await _reducer.WaitTillSubscribed(startupTimeout);
        await _reducer.WaitTillActive(startupTimeout);
    }

    async Task Because()
    {
        const string eventSourceId = "some source";
        var result = await EventStore.EventLog.Append(eventSourceId, new SomeEvent(42));
        await _reducer.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
        CountAfterFirstEvent = Interlocked.Read(ref _netCount);

        result = await EventStore.EventLog.Append(eventSourceId, new SomeDeleteEvent());
        await _reducer.WaitTillReachesEventSequenceNumber(result.SequenceNumber);

        CountAfterSecondEvent = Interlocked.Read(ref _netCount);
    }

    void Destroy() => _subscription?.Dispose();

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reducer = new TReducer();
        services.AddSingleton(Reducer);
    }
}
