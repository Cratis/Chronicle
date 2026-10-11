// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

using context = Cratis.Chronicle.Integration.for_Reducers.when_generation_is_pinned.and_an_earlier_generation_has_personal_data.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_generation_is_pinned;

[Collection(ChronicleCollection.Name)]
public class and_an_earlier_generation_has_personal_data(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : for_Reactors.when_generation_is_pinned.given.a_pinned_client(fixture)
    {
        readonly TaskCompletionSource<(PinnedPersonRegistered Event, EventContext Context)> _live = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<(PinnedPersonRegistered Event, EventContext Context)> _replay = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public (PinnedPersonRegistered Event, EventContext Context) Live;
        public (PinnedPersonRegistered Event, EventContext Context) Replayed;
        public override IEnumerable<Type> EventTypes => [typeof(PinnedPersonRegisteredV1), typeof(PinnedPersonRegistered)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(PinnedPersonMigration)];
        public override IEnumerable<Type> Reducers => [typeof(PinnedReducer)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new PinnedReducer(_live, _replay));
        }

        async Task Because()
        {
            await _pinnedStore.Reducers.GetHandlerFor<PinnedReducer>().WaitTillSubscribed();
            var append = await _pinnedStore.EventLog.Append($"person-{Guid.NewGuid()}", new PinnedPersonRegisteredV1("Ada Lovelace"));
            (await append.WaitForCompletion()).IsSuccess.ShouldBeTrue();
            Live = await _live.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var replay = await _pinnedStore.Reducers.Replay<PinnedReducer>();
            Replayed = await _replay.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await _pinnedStore.Jobs.WaitTillJobCompletesOrIsDeleted(replay, TimeSpan.FromSeconds(30));
        }
    }

    [Fact] void should_release_personal_data_live() => Context.Live.Event.FullName.ShouldEqual("Ada Lovelace");
    [Fact] void should_release_the_same_shape_on_replay() => Context.Replayed.Event.ShouldEqual(Context.Live.Event);
    [Fact] void should_deliver_generation_two_live() => Context.Live.Context.EventType.Generation.Value.ShouldEqual(2U);
    [Fact] void should_deliver_generation_two_on_replay() => Context.Replayed.Context.EventType.Generation.Value.ShouldEqual(2U);
    [Fact] void should_preserve_the_appended_generation_live() => Context.Live.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_preserve_the_appended_generation_on_replay() => Context.Replayed.Context.AppendedGeneration!.Value.ShouldEqual(1U);
}
