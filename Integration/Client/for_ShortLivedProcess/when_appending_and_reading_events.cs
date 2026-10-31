// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration.given;

using context = Cratis.Chronicle.Integration.for_ShortLivedProcess.when_appending_and_reading_events.context;

namespace Cratis.Chronicle.Integration.for_ShortLivedProcess;

[Collection(ChronicleCollection.Name)]
public class when_appending_and_reading_events(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public bool AppendSucceeded;
        public int EventsRead;
        public string EventTypeId;
        public bool ObserverRegistrationFailedAsExpected;
        public bool OutOfProcess;

        async Task Because()
        {
            OutOfProcess = ChronicleFixture.Options.Mode == ChronicleRuntimeMode.OutOfProcess;
            var connectionString = new ChronicleOptions().ConnectionString;
            if (OutOfProcess)
            {
                connectionString = new ChronicleConnectionString($"chronicle://localhost:{ChronicleFixture.KernelGrpcHostPort}/?skipTlsValidation=true");
            }
            var options = ChronicleOptions.ForShortLivedProcess(connectionString, RegistrationMode.EventTypesOnly);
            options.AutoDiscoverAndRegister = false;
            options.ExplicitArtifacts.RegisterEventType<ShortLivedHappened>(builder => builder.WithId("5113-short-lived-happened"));

            using var client = OutOfProcess
                ? new ChronicleClient(options, artifactsProvider: this)
                : new ChronicleClient(new BorrowedConnection(EventStore.Connection), options, artifactsProvider: this);
            var store = await client.GetEventStore($"short-lived-{Guid.NewGuid():N}");
            await store.DiscoverAll();
            await store.RegisterAll();

            var append = await store.EventLog.Append("source", new ShortLivedHappened("hello"));
            AppendSucceeded = append.IsSuccess;

            var events = await store.EventLog.GetForEventSourceIdAndEventTypes("source", [store.EventTypes.GetEventTypeFor(typeof(ShortLivedHappened))]);
            EventsRead = events.Count;
            EventTypeId = store.EventTypes.GetEventTypeFor(typeof(ShortLivedHappened)).Id.Value;

            // A borrowed in-process connection is not keep-alive aware, so only the out-of-process client refuses observers.
            var error = await Catch.Exception(() => store.Reactors.Register(
                "5113-short-lived-reactor",
                builder => builder.WithEventType(store.EventTypes.GetEventTypeFor(typeof(ShortLivedHappened))),
                (_, _) => Task.CompletedTask));
            ObserverRegistrationFailedAsExpected = OutOfProcess ? error is ClientObserversRequireKeepAlive : error is null;
            client.EvictEventStores();
        }
    }

    [Fact] void should_append_the_event() => Context.AppendSucceeded.ShouldBeTrue();
    [Fact] void should_read_the_event_back() => Context.EventsRead.ShouldEqual(1);
    [Fact] void should_use_the_explicit_event_type_identity() => Context.EventTypeId.ShouldEqual("5113-short-lived-happened");
    [Fact] void should_refuse_observers_only_when_keep_alive_is_skipped() => Context.ObserverRegistrationFailedAsExpected.ShouldBeTrue();

    public record ShortLivedHappened(string Message);
}
