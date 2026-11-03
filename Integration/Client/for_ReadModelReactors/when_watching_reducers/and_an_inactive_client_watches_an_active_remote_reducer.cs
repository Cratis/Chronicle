// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers.and_an_inactive_client_watches_an_active_remote_reducer.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors.when_watching_reducers;

[Collection(ChronicleCollection.Name)]
public class and_an_inactive_client_watches_an_active_remote_reducer(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_reducer(fixture)
    {
        public int RemoteCallbacks;
        public ReadModelChangeset<ReducerWatchedReadModel> LocalCallback;
        public bool BorrowedHandlerActive;

        async Task Because()
        {
            using var client = new BorrowedReducerClient(EventStore);
            var store = await client.Open(EventStore);
            BorrowedHandlerActive = store.Reducers.GetHandlerFor<InactiveWatchedReducer>().IsActive;
            using var local = new Collector(store.ReadModels.Watch<ReducerWatchedReadModel>());
            await Handshake();
            await Change("remote", 1, "remote state");
            await Change("remote", 2, "remote barrier");
            RemoteCallbacks = local.For("remote").Length;
            await store.ReadModels.GetInstanceById<ReducerWatchedReadModel>("remote");
            LocalCallback = await local.WaitForSequence("remote", EventSequenceNumber.Unavailable, Token);
        }
    }

    [Fact] void should_keep_the_borrowed_handler_inactive() => Context.BorrowedHandlerActive.ShouldBeFalse();
    [Fact] void should_not_treat_remote_changes_as_local_callbacks() => Context.RemoteCallbacks.ShouldEqual(0);
    [Fact] void should_still_receive_local_folds_as_modified() => Context.LocalCallback.ChangeType.ShouldEqual(ReadModelChangeType.Modified);
    [Fact] void should_keep_the_local_callback_context_reduced() => Context.LocalCallback.ChangeContext!.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_keep_the_active_watch_working() => Context.Watch.For("remote").Length.ShouldEqual(2);
}
