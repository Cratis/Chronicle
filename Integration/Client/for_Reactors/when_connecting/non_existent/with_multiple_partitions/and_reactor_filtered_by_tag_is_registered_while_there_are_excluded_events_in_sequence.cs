// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_connecting.non_existent.with_multiple_partitions.and_reactor_filtered_by_tag_is_registered_while_there_are_excluded_events_in_sequence.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_connecting.non_existent.with_multiple_partitions;

[Collection(ChronicleCollection.Name)]
public class and_reactor_filtered_by_tag_is_registered_while_there_are_excluded_events_in_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public ReactorCapturingAuditedEvents Reactor;
        public IAppendResultForObserverCompletion AuditedAppend;
        public IAppendResultForObserverCompletion ExcludedAppend;
        public AppendResultWaitForCompletionResult ExcludedCompletion;
        public ReactorState ReactorState;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new();
            services.AddSingleton(Reactor);
        }

        async Task Establish()
        {
            AuditedAppend = await EventStore.EventLog.Append("audited-source", new SomeEvent(1), tags: ["audited"]);
            ExcludedAppend = await EventStore.EventLog.Append("excluded-source", new SomeEvent(2));
        }

        async Task Because()
        {
            // Both events were appended before the reactor registered, so they reach it through catch-up rather than
            // live delivery. Catch-up must apply the same filter: the tagged event is delivered, the untagged one never
            // is, and the reactor moves past the excluded event so completion does not wait for it to be handled.
            var reactor = await EventStore.Reactors.Register<ReactorCapturingAuditedEvents>();
            await reactor.WaitTillSubscribed();
            ExcludedCompletion = await ExcludedAppend.WaitForCompletion(TimeSpanFactory.DefaultTimeout());
            await AuditedAppend.WaitForCompletion(TimeSpanFactory.DefaultTimeout());
            ReactorState = await reactor.WaitTillMovesPastEventSequenceNumber(ExcludedAppend.TailSequenceNumber, TimeSpanFactory.DefaultTimeout());
        }
    }

    [Fact] void should_deliver_only_the_tagged_event() => Context.Reactor.Numbers.ToArray().ShouldEqual([1]);
    [Fact] void should_complete_the_excluded_append_without_it_being_handled() => Context.ExcludedCompletion.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_only_the_tagged_event_as_handled() => Context.ReactorState.LastHandledEventSequenceNumber.ShouldEqual(Context.AuditedAppend.TailSequenceNumber);
    [Fact] void should_move_past_the_excluded_event() => Context.ReactorState.NextEventSequenceNumber.ShouldEqual(Context.ExcludedAppend.TailSequenceNumber.Next());
    [Fact] void should_be_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);
}
