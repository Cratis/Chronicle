// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Sinks;

using context = Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers.and_explicit_clients_use_the_selected_kernel_transport.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers;

[Collection(ChronicleCollection.Name)]
public class and_explicit_clients_use_the_selected_kernel_transport(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public int[] Totals;
        public bool UsesRemoteTransport;
        public bool OutOfProcess;
        public string RegisteredId;
        public string EventId;

        async Task Because()
        {
            OutOfProcess = ChronicleFixture.Options.Mode == ChronicleRuntimeMode.OutOfProcess;
            var options = new ChronicleOptions
            {
                AutoDiscoverAndRegister = false,
                DefaultSinkTypeId = ChronicleFixture.Options.StorageProvider == ChronicleStorageProvider.MongoDB ? WellKnownSinkTypes.MongoDB : WellKnownSinkTypes.SQL
            };
            options.ExplicitArtifacts.RegisterEventType<Added>(builder => builder.WithId("4731-transport-added"))
                .RegisterReducer<SelfTotal>("4731-transport-self").RegisterReducer<Classic>("4731-transport-classic");
            if (OutOfProcess)
            {
                // Do not borrow the fixture's local silo connection for a remote transport assertion.
                options.ConnectionString = new ChronicleConnectionString($"chronicle://localhost:{ChronicleFixture.KernelGrpcHostPort}/?skipTlsValidation=true");
            }
            using var client = OutOfProcess
                ? new ChronicleClient(options, artifactsProvider: this)
                : new ChronicleClient(new BorrowedConnection(EventStore.Connection), options, artifactsProvider: this);
            var store = await client.GetEventStore($"transport-{Guid.NewGuid():N}");
            await store.DiscoverAll();
            await store.RegisterAll();
            store.Registration.IsSuccess.ShouldBeTrue();
            UsesRemoteTransport = store.Connection is not BorrowedConnection;
            var handlers = new[] { store.Reducers.GetHandlerFor(typeof(SelfTotal)), store.Reducers.GetHandlerFor<Classic>() };
            foreach (var handler in handlers) await handler.WaitTillSubscribed();
            var append = await store.EventLog.Append("source", new Added(42));
            (await append.WaitForCompletion()).IsSuccess.ShouldBeTrue();
            var job = await store.Reducers.Replay(typeof(SelfTotal));
            job.ShouldNotEqual(JobId.NotSet);
            await store.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await handlers[0].WaitTillActive();
            Totals = [(await store.ReadModels.GetInstanceById<SelfTotal>("source")).Total, (await store.ReadModels.GetInstanceById<ClassicTotal>("source")).Total];
            RegisteredId = handlers[0].Id.Value;
            EventId = store.EventTypes.GetEventTypeFor(typeof(Added)).Id.Value;
            client.EvictEventStores();
        }
    }

    [Fact] void should_use_remote_grpc_rather_than_the_local_fixture_silo_in_outofprocess_lanes() => Context.UsesRemoteTransport.ShouldEqual(Context.OutOfProcess);
    [Fact] void should_materialize_both_states_in_the_selected_storage_sink() => Context.Totals.ShouldEqual<int>([42, 42]);
    [Fact] void should_preserve_explicit_self_reducer_identity() => Context.RegisteredId.ShouldEqual("4731-transport-self");
    [Fact] void should_preserve_explicit_event_identity() => Context.EventId.ShouldEqual("4731-transport-added");

    public record Added(int Amount);
    public record SelfTotal(int Total)
    {
        public static SelfTotal Reduce(Added e, SelfTotal? current) => new((current?.Total ?? 0) + e.Amount);
    }
    public record ClassicTotal(int Total);
    public class Classic : IReducerFor<ClassicTotal>
    {
        public ClassicTotal Apply(Added e, ClassicTotal? current) => new((current?.Total ?? 0) + e.Amount);
    }
}
