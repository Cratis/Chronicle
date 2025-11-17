// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration.Specifications.for_Reducers.given;

public class a_reducer_able_to_delete<TReducer>(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    where TReducer : class, IReducerFor<SomeReadModel>, new()
{
    public TReducer Reducer;
    public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(SomeDeleteEvent)];
    public override IEnumerable<Type> Reducers => [typeof(TReducer)];

    public long CountAfterFirstEvent;
    public long CountAfterSecondEvent;

    protected IMongoCollection<SomeReadModel> _collection => ReadModelsDatabase.Database.GetCollection<SomeReadModel>();
    protected IReducerHandler _reducer;

    async Task Establish()
    {
        _reducer = EventStore.Reducers.GetHandlerFor<TReducer>();
        await _reducer.WaitTillActive();
    }

    async Task Because()
    {
        const string eventSourceId = "some source";
        var result = await EventStore.EventLog.Append(eventSourceId, new SomeEvent(42));
        await _reducer.WaitTillReachesEventSequenceNumber(result.SequenceNumber);
        CountAfterFirstEvent = await _collection.CountDocumentsAsync(Builders<SomeReadModel>.Filter.Empty);

        result = await EventStore.EventLog.Append(eventSourceId, new SomeDeleteEvent());
        await _reducer.WaitTillReachesEventSequenceNumber(result.SequenceNumber);

        CountAfterSecondEvent = await _collection.CountDocumentsAsync(Builders<SomeReadModel>.Filter.Empty);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        Reducer = new TReducer();
        services.AddSingleton(Reducer);
    }
}
