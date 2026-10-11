// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Threading.Channels;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Webhooks;

using context = Cratis.Chronicle.Integration.for_Webhooks.when_generation_is_pinned.and_the_same_event_is_delivered_on_every_path.context;

namespace Cratis.Chronicle.Integration.for_Webhooks.when_generation_is_pinned;

[Collection(ChronicleCollection.Name)]
public class and_the_same_event_is_delivered_on_every_path(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.all_dependencies(fixture)
    {
        readonly Channel<JsonObject> _deliveries = Channel.CreateUnbounded<JsonObject>();
        readonly WebhookId _id = $"pinned-webhook-{Guid.NewGuid():N}";
        readonly string _source = $"person-{Guid.NewGuid():N}";
        bool _failDelivery;
        public JsonObject CatchUp;
        public JsonObject Live;
        public JsonObject Replayed;
        public JsonObject Retried;
        public override IEnumerable<Type> EventTypes => [typeof(PinnedPersonRegisteredV1), typeof(PinnedPersonRegistered)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(PinnedPersonMigration)];

        protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
        {
            builder.Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapPost("/webhooks", async httpContext =>
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                var payload = JsonNode.Parse(await reader.ReadToEndAsync())!.AsObject();
                foreach (var @event in payload["events"]!.AsArray())
                {
                    await _deliveries.Writer.WriteAsync(@event!.AsObject());
                }

                httpContext.Response.StatusCode = _failDelivery ? 400 : 200;
            })));
        }

        async Task<JsonObject> NextDelivery()
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return await _deliveries.Reader.ReadAsync(cancellation.Token);
        }

        async Task Register(WebhookId id)
        {
            await Webhooks.Register(id, TargetUrl, builder => builder.WithEventType<PinnedPersonRegistered>().WithGenerationDelivery(EventGenerationDelivery.Pinned));
            var registration = await EventStore.Reactors.WaitForHandlerById("$system.Cratis.Chronicle.Observation.Webhooks.WebhookReactor", TimeSpan.FromSeconds(30));
            var tail = (await EventStore.GetEventSequence(EventSequenceId.System).GetTailSequenceNumber()).Value;
            await registration.WaitTillReachesEventSequenceNumber(tail);
        }

        async Task Because()
        {
            await Register(_id);
            _failDelivery = true;
            var appended = await EventStore.EventLog.Append(_source, new PinnedPersonRegisteredV1("Ada Lovelace"));
            Live = await NextDelivery();
            (await appended.WaitForCompletion()).IsSuccess.ShouldBeFalse();
            _failDelivery = false;

            var observers = ((IChronicleServicesAccessor)EventStore.Connection).Services.Observers;
            await observers.RetryPartition(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                EventSequenceId = EventSequenceId.Log,
                ObserverId = _id,
                Partition = _source
            });
            Retried = await NextDelivery();
            (await appended.WaitForCompletion()).IsSuccess.ShouldBeTrue();

            var replay = await observers.Replay(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                EventSequenceId = EventSequenceId.Log,
                ObserverId = _id
            });
            Replayed = await NextDelivery();
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(new JobId(Guid.Parse(replay.JobId)), TimeSpan.FromSeconds(30));

            // A second webhook starts after the append, exercising catch-up for that same stored event.
            await Register($"catch-up-{Guid.NewGuid():N}");
            CatchUp = await NextDelivery();
        }
    }

    [Fact] void should_release_personal_data_on_catch_up() => Context.CatchUp["content"]!["fullName"]!.GetValue<string>().ShouldEqual("Ada Lovelace");
    [Fact] void should_deliver_identical_content_live() => Context.Live["content"]!.ToJsonString().ShouldEqual(Context.CatchUp["content"]!.ToJsonString());
    [Fact] void should_deliver_identical_content_on_replay() => Context.Replayed["content"]!.ToJsonString().ShouldEqual(Context.CatchUp["content"]!.ToJsonString());
    [Fact] void should_deliver_identical_content_on_retry() => Context.Retried["content"]!.ToJsonString().ShouldEqual(Context.CatchUp["content"]!.ToJsonString());
    [Fact] void should_keep_generation_two_on_every_path() => new[] { Context.CatchUp, Context.Live, Context.Replayed, Context.Retried }.All(_ => _["context"]!["eventType"]!["generation"]!.GetValue<uint>() == 2u).ShouldBeTrue();
    [Fact] void should_keep_appended_generation_one_on_every_path() => new[] { Context.CatchUp, Context.Live, Context.Replayed, Context.Retried }.All(_ => _["context"]!["appendedGeneration"]!.GetValue<uint>() == 1u).ShouldBeTrue();
    [Fact] void should_preserve_the_hash_on_catch_up() => Context.CatchUp["context"]!["hash"]!.ToJsonString().ShouldEqual(Context.Live["context"]!["hash"]!.ToJsonString());
    [Fact] void should_preserve_the_hash_on_replay() => Context.Replayed["context"]!["hash"]!.ToJsonString().ShouldEqual(Context.Live["context"]!["hash"]!.ToJsonString());
    [Fact] void should_preserve_the_hash_on_retry() => Context.Retried["context"]!["hash"]!.ToJsonString().ShouldEqual(Context.Live["context"]!["hash"]!.ToJsonString());
}
