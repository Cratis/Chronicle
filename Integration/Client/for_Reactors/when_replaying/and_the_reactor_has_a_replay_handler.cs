// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_replaying.and_the_reactor_has_a_replay_handler.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_replaying;

/// <summary>
/// The replay handler is selected from EventObservationState on the event context, which the kernel stamps and
/// ships to the client over gRPC. In-process specs take that context directly, so this is the only place the
/// signal is proven to survive the wire during a real replay.
/// </summary>
/// <param name="context">The <see cref="context"/> the specs run against.</param>
[Collection(ChronicleCollection.Name)]
public class and_the_reactor_has_a_replay_handler(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId;
        public ReactorWithReplayHandler Reactor;
        public int LiveHandledBeforeReplay;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorWithReplayHandler)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new ReactorWithReplayHandler();
            services.AddSingleton(Reactor);
        }

        void Establish() => EventSourceId = "some source";

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorWithReplayHandler>();
            await reactor.WaitTillActive();

            await EventStore.EventLog.Append(EventSourceId, new SomeEvent(42));
            await Reactor.WaitTillLiveHandledReaches(1);
            LiveHandledBeforeReplay = Reactor.LiveHandled;

            var replayJobId = await EventStore.Reactors.Replay<ReactorWithReplayHandler>();
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(replayJobId);
            await Reactor.WaitTillReplayHandledReaches(1);
        }
    }

    [Fact] void should_handle_the_event_live_when_it_is_appended() => Context.LiveHandledBeforeReplay.ShouldEqual(1);

    [Fact] void should_handle_the_event_with_the_replay_handler_during_the_replay() => Context.Reactor.ReplayHandled.ShouldEqual(1);

    [Fact] void should_not_handle_it_with_the_live_handler_again_during_the_replay() => Context.Reactor.LiveHandled.ShouldEqual(1);
}
