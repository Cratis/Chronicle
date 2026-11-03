// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using context = Cratis.Chronicle.Integration.for_Observers.when_a_slow_reactor_shares_a_queue_with_a_reducer.and_events_are_appended_across_partitions.context;

namespace Cratis.Chronicle.Integration.for_Observers.when_a_slow_reactor_shares_a_queue_with_a_reducer;

/// <summary>
/// A reactor that is still waiting on its first event must not hold back a reducer that the kernel routes to the same
/// appended-events queue: the reducer reaches the tail on its own pace.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_events_are_appended_across_partitions(context context) : Given<context>(context)
{
    const int Partitions = 5;
    const int EventsPerPartition = 4;
    const int TotalEvents = Partitions * EventsPerPartition;

    public class context(ChronicleFixture chronicleFixture) : given.a_slow_reactor_and_a_reducer_on_the_same_queue(chronicleFixture)
    {
        public int ReducedWhileReactorWasBlocked;
        public int ReactorHandledWhenReducerWasDone;

        async Task Because()
        {
            var timeout = TimeSpanFactory.FromSeconds(30);
            await EventStore.Reactors.GetHandlerFor<GatedReactor>().WaitTillActive(timeout);
            await EventStore.Reducers.GetHandlerFor<CountingReducer>().WaitTillActive(timeout);

            try
            {
                // Interleaved partitions, as a burst of writes from many event sources produces.
                for (var round = 0; round < EventsPerPartition; round++)
                {
                    for (var partition = 0; partition < Partitions; partition++)
                    {
                        await EventStore.EventLog.Append($"item-source-{partition}", new ItemRegistered($"item-{round}-{partition}"));
                    }
                }

                // Well inside the kernel's subscriber timeout, so the reactor's first event is still unanswered.
                await Reducer.WaitTillHandled(TotalEvents, TimeSpan.FromSeconds(10));
                ReducedWhileReactorWasBlocked = Reducer.Handled;
                ReactorHandledWhenReducerWasDone = Reactor.Handled;
            }
            finally
            {
                Reactor.Open();
            }
        }
    }

    [Fact] void should_let_the_reducer_reach_the_tail_while_the_reactor_is_blocked() => Context.ReducedWhileReactorWasBlocked.ShouldEqual(TotalEvents);
    [Fact] void should_still_have_the_reactor_blocked_on_its_first_event() => Context.ReactorHandledWhenReducerWasDone.ShouldEqual(0);
}
