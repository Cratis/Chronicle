// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reducers;

using context = Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers.and_pinned_generations_release_personal_data.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers;

[Collection(ChronicleCollection.Name)]
public class and_pinned_generations_release_personal_data(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : for_Reactors.when_generation_is_pinned.given.a_pinned_client(fixture)
    {
        readonly EventSourceId _source = EventSourceId.New();
        public SelfPerson Live;
        public ClassicPerson ClassicLive;
        public SelfPerson Replayed;
        public ClassicPerson ClassicReplayed;
        public override IEnumerable<Type> EventTypes => [typeof(PinnedPersonRegisteredV1), typeof(PinnedPersonRegistered)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(PinnedPersonMigration)];
        public override IEnumerable<Type> Reducers => [typeof(SelfPerson), typeof(Classic)];

        async Task Because()
        {
            var handlers = new[] { _pinnedStore.Reducers.GetHandlerFor(typeof(SelfPerson)), _pinnedStore.Reducers.GetHandlerFor<Classic>() };
            foreach (var handler in handlers) await handler.WaitTillSubscribed();
            var append = await _pinnedStore.EventLog.Append(_source, new PinnedPersonRegisteredV1("Ada Lovelace"));
            (await append.WaitForCompletion()).IsSuccess.ShouldBeTrue();
            Live = await _pinnedStore.ReadModels.GetInstanceById<SelfPerson>(_source);
            ClassicLive = await _pinnedStore.ReadModels.GetInstanceById<ClassicPerson>(_source);
            foreach (var handler in handlers)
            {
                var replay = await _pinnedStore.Reducers.Replay(handler.Id);
                replay.ShouldNotEqual(JobId.NotSet);
                await _pinnedStore.Jobs.WaitTillJobCompletesOrIsDeleted(replay);
                await handler.WaitTillActive();
            }
            Replayed = await _pinnedStore.ReadModels.GetInstanceById<SelfPerson>(_source);
            ClassicReplayed = await _pinnedStore.ReadModels.GetInstanceById<ClassicPerson>(_source);
        }
    }

    [Fact] void should_release_and_migrate_before_the_self_fold() => Context.Live.SawPlainText.ShouldBeTrue();
    [Fact] void should_release_the_persisted_self_model() => Context.Live.FullName.ShouldEqual("Ada Lovelace");
    [Fact] void should_share_the_classic_live_content() => Context.Live.FullName.ShouldEqual(Context.ClassicLive.FullName);
    [Fact] void should_deliver_the_registered_generation_live() => Context.Live.Generation.ShouldEqual(2U);
    [Fact] void should_preserve_the_appended_generation_live() => Context.Live.AppendedGeneration.ShouldEqual(1U);
    [Fact] void should_release_personal_data_on_replay() => Context.Replayed.SawPlainText.ShouldBeTrue();
    [Fact] void should_share_classic_replay_content_and_context() => Context.Replayed.ShouldEqual(new SelfPerson(Context.ClassicReplayed.FullName, Context.ClassicReplayed.SawPlainText, Context.ClassicReplayed.Generation, Context.ClassicReplayed.AppendedGeneration));
    [Fact] void should_preserve_live_and_replay_generation_parity() => Context.Replayed.ShouldEqual(Context.Live);

    public record SelfPerson([property: PII] string FullName, bool SawPlainText, uint Generation, uint AppendedGeneration)
    {
        public static SelfPerson Reduce(PinnedPersonRegistered e, SelfPerson? current, EventContext context) =>
            new(e.FullName, e.FullName == "Ada Lovelace", context.EventType.Generation.Value, context.AppendedGeneration!.Value);
    }
    public record ClassicPerson([property: PII] string FullName, bool SawPlainText, uint Generation, uint AppendedGeneration);
    public class Classic : IReducerFor<ClassicPerson>
    {
        public ClassicPerson Apply(PinnedPersonRegistered e, ClassicPerson? current, EventContext context) =>
            new(e.FullName, e.FullName == "Ada Lovelace", context.EventType.Generation.Value, context.AppendedGeneration!.Value);
    }
}
