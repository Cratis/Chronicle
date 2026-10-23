// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_replaying.and_the_reactor_observes_an_inbox.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_replaying;

[Collection(ChronicleCollection.Name)]
public class and_the_reactor_observes_an_inbox(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public InboxReplayReactor Reactor;
        public JobId ReplayJobId;
        public ReactorState StateAfterReplay;
        public int LiveHandledBeforeReplay;
        public EventSequenceId ObservedSequence;

        public override IEnumerable<Type> EventTypes => [typeof(InboxReplayEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new InboxReplayReactor();
            services.AddSingleton(Reactor);
        }

        async Task Because()
        {
            var source = await ChronicleClient.GetEventStore(InboxReplayReactor.SourceEventStore);
            await source.RegisterAll();
            var handler = await EventStore.Reactors.Register<InboxReplayReactor>();
            ObservedSequence = handler.EventSequenceId;
            await handler.WaitTillActive();
            await EventStore.Subscriptions.Subscribe("inbox-replay", InboxReplayReactor.SourceEventStore,
                builder => builder.WithEventType<InboxReplayEvent>());

            var outbox = source.GetEventSequence(EventSequenceId.Outbox);
            await outbox.Append("first-source", new InboxReplayEvent(1));
            await outbox.Append("second-source", new InboxReplayEvent(2));
            await Reactor.WaitTillHandled(2);
            LiveHandledBeforeReplay = Volatile.Read(ref Reactor.LiveHandled);

            // This client sends an empty sequence, so the server must resolve the registered inbox.
            ReplayJobId = await EventStore.Reactors.Replay<InboxReplayReactor>();
            ReplayJobId.ShouldNotEqual(JobId.NotSet);
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(ReplayJobId);
            await Reactor.WaitTillHandled(2, replay: true);
            StateAfterReplay = await handler.WaitTillActiveAndGetState();
        }
    }

    [Fact] void should_observe_the_sources_inbox() => Context.ObservedSequence.Value.ShouldEqual("inbox-reactor-replay-source");
    [Fact] void should_receive_events_live_from_the_subscription() => Context.LiveHandledBeforeReplay.ShouldEqual(2);
    [Fact] void should_start_a_replay_job() => Context.ReplayJobId.ShouldNotEqual(JobId.NotSet);
    [Fact] void should_rehandle_the_inbox_events() => Context.Reactor.ReplayHandled.ShouldEqual(2);
    [Fact] void should_mark_replayed_events_as_replay() => Context.Reactor.ReplayObservationState.HasFlag(EventObservationState.Replay).ShouldBeTrue();
    [Fact] void should_not_deliver_replayed_events_as_live() => Context.Reactor.LiveHandled.ShouldEqual(2);
    [Fact] void should_be_active_after_replay() => Context.StateAfterReplay.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_still_be_subscribed_after_replay() => Context.StateAfterReplay.IsSubscribed.ShouldBeTrue();
}
