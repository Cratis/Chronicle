// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_ReadModelReactors.when_another_client_materializes_an_inactive_model.context;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[Collection(ChronicleCollection.Name)]
public class when_another_client_materializes_an_inactive_model(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public override bool AutoDiscoverArtifacts => false;
        public override IEnumerable<Type> EventTypes => [typeof(Reduction)];
        public readonly ReducerFrames InactiveWatch = new();
        public readonly InactiveReducerFrames InactiveReactor = new();
        public readonly ReducerFrames ActiveWatch = new();
        public readonly ReducerFrames ActiveReactor = new();
        public bool InactiveHandler;
        public bool ActiveHandler;
        public ReducedModel Stored;

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(ActiveReactor);
            services.AddSingleton(InactiveReactor);
            services.AddSingleton<ReducerRecordingReactor<ReducedModel>>();
            services.AddSingleton<InactiveRecordingReactor<ReducedModel>>();
            services.AddSingleton<WatchedReducer<ReducedModel>>();
            services.AddSingleton<InactiveWatchedReducer<ReducedModel>>();
        }

        async Task Because()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var inactive = CreateClient(false);
            using var active = CreateClient(true);
            var localStore = await inactive.GetEventStore(EventStore.Name, EventStore.Namespace);
            await localStore.DiscoverAll();
            await localStore.RegisterAll();
            InactiveHandler = localStore.Reducers.GetHandlerFor<InactiveWatchedReducer<ReducedModel>>().IsActive;
            using var localWatch = localStore.ReadModels.Watch<ReducedModel>().Subscribe(change => InactiveWatch.Record(change.ChangeType, change.ChangeContext!));
            var activeStore = await active.GetEventStore(EventStore.Name, EventStore.Namespace);
            await activeStore.DiscoverAll();
            await activeStore.RegisterAll();
            using var remoteWatch = activeStore.ReadModels.Watch<ReducedModel>().Subscribe(change => ActiveWatch.Record(change.ChangeType, change.ChangeContext!));
            await ObserveRemoteWrite(activeStore, timeout.Token);
            inactive.EvictEventStores();
            active.EvictEventStores();
        }

        async Task ObserveRemoteWrite(IEventStore store, CancellationToken token)
        {
            var handler = store.Reducers.GetHandlerFor<WatchedReducer<ReducedModel>>();
            ActiveHandler = handler.IsActive;
            await handler.WaitTillSubscribed().WaitAsync(token);
            await ReducerWatchReadiness.WaitFor(store, ActiveWatch, ActiveReactor, token);
            var result = await store.EventLog.Append("remote", new Reduction(7));
            await ActiveWatch.WaitFor("remote", ReadModelChangeType.Added, result.SequenceNumber, token);
            await ActiveReactor.WaitFor("remote", ReadModelChangeType.Added, result.SequenceNumber, token);
            await handler.WaitTillActive().WaitAsync(token);
            Stored = await store.ReadModels.GetInstanceById<ReducedModel>("remote");
        }

        ChronicleClient CreateClient(bool active) => new(
            new BorrowedConnection(EventStore.Connection),
            new ChronicleOptions { AutoDiscoverAndRegister = false, DefaultSinkTypeId = ChronicleClient.Options.DefaultSinkTypeId },
            artifactsProvider: new ReducerClientArtifacts(active),
            serviceProvider: Services);
    }

    [Fact] void should_keep_the_first_clients_handler_inactive() => Context.InactiveHandler.ShouldBeFalse();
    [Fact] void should_activate_the_materializing_clients_handler() => Context.ActiveHandler.ShouldBeTrue();
    [Fact] void should_materialize_the_same_model_on_the_other_client() => Context.Stored.Value.ShouldEqual(7);
    [Fact] void should_not_deliver_remote_writes_to_the_inactive_watch() => Context.InactiveWatch.For("remote").ShouldBeEmpty();
    [Fact] void should_not_dispatch_remote_writes_to_the_inactive_reactor() => Context.InactiveReactor.For("remote").ShouldBeEmpty();
}
