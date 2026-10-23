// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_interleaved_event_sources;
using Cratis.Chronicle.Jobs;
using Microsoft.Extensions.Options;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_in_buckets.and_the_documents_equal_ordered_live_handling.context;
using EventSequenceNumber = Cratis.Chronicle.Events.EventSequenceNumber;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_in_buckets;

[Collection(ChronicleCollection.Name)]
public class and_the_documents_equal_ordered_live_handling(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        readonly Guid[] _ids = Enumerable.Range(0, 13).Select(_ => Guid.NewGuid()).ToArray();

        public Member[] Live = [];
        public Member[] Replayed = [];
        public int ConfiguredBuckets;

        public override IEnumerable<Type> EventTypes => [typeof(UserCreated)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(Member)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jobs:ProjectionRebuildBuckets"] = "8",
                ["ReadModels:GuardSinkWritesOnWatermark"] = "true"
            }).Build();
            services.Configure<Configuration.ChronicleOptions>(configuration);
        }

        async Task Because()
        {
            ConfiguredBuckets = Services.GetRequiredService<IOptions<Configuration.ChronicleOptions>>().Value.Jobs.ProjectionRebuildBuckets;
            var handler = EventStore.Projections.GetAllHandlers().Single();
            await handler.WaitTillSubscribed();
            var last = EventSequenceNumber.Unavailable;
            for (var round = 0; round < 3; round++)
            {
                foreach (var id in _ids) last = (await EventStore.EventLog.Append(id, new UserCreated($"{id:N}-{round}"))).SequenceNumber;
            }

            await handler.WaitTillReachesEventSequenceNumber(last);
            Live = await ReadAll();
            var job = await EventStore.Projections.Replay(handler.Id);
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await handler.WaitTillActive();
            Replayed = await ReadAll();
        }

        async Task<Member[]> ReadAll()
        {
            var documents = new List<Member>();
            foreach (var id in _ids) documents.Add(await EventStore.ReadModels.GetInstanceById<Member>(id.ToString()));
            return documents.ToArray();
        }
    }

    [Fact] void should_enable_eight_buckets() => Context.ConfiguredBuckets.ShouldEqual(8);
    [Fact] void should_materialize_every_live_partition() => Context.Live.Length.ShouldEqual(13);
    [Fact] void should_keep_every_interleaved_partition_in_order() => Context.Live.All(member => member.Name.EndsWith("-2", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_rebuild_every_field_identically() => Assert.Equivalent(Context.Live, Context.Replayed, strict: true);
}
