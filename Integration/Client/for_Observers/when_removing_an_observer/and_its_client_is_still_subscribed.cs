// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Observers.when_removing_an_observer.and_its_client_is_still_subscribed.context;

// The kernel's Core assembly declares its own ObserverRemovalResult, and this project references both it and the
// client. The client's is the one a consumer sees, so it is the one specified here.
using ObserverRemovalResult = Cratis.Chronicle.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Integration.for_Observers.when_removing_an_observer;

/// <summary>
/// The guard is the part of removal that has to hold against a real kernel. A unit specification can only show that
/// the remover asks the observer grain whether it is subscribed; whether a reactor that a connected client is
/// currently reporting actually answers yes is a property of the running system, and it is the difference between
/// refusing and tearing a live observer out from under a running application.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_its_client_is_still_subscribed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public ReactorWithoutDelay Reactor = default!;
        public ObserverRemovalResult Result = default!;
        public ReactorState StateAfterRefusedRemoval = default!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorWithoutDelay)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new();
            services.AddSingleton(Reactor);
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            await reactor.WaitTillActive();

            Result = await EventStore.Observers.Remove(reactor.Id.Value);

            // The observer has to be left working, not merely left present. A refusal that still stood the grain
            // down or deleted half its records would report success at being safe while having already done harm.
            var appendResult = await EventStore.EventLog.Append("some-source", new SomeEvent(1));
            await reactor.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);
            await Reactor.WaitTillHandledEventReaches(1);
            StateAfterRefusedRemoval = await reactor.WaitTillActiveAndGetState(TimeSpanFactory.FromSeconds(30));
        }
    }

    [Fact] void should_refuse_the_removal() => Context.Result.IsRemoved.ShouldBeFalse();

    [Fact] void should_report_that_a_client_is_still_subscribed() => Context.Result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverSubscribed);

    [Fact] void should_name_the_namespace_that_blocked_it() => Context.Result.BlockingNamespace.ShouldEqual(Context.EventStore.Namespace.Value);

    [Fact] void should_leave_the_observer_active() => Context.StateAfterRefusedRemoval.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact] void should_leave_the_observer_handling_events() => Context.Reactor.HandledEvents.ShouldEqual(1);
}
