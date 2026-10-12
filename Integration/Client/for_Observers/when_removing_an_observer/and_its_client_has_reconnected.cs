// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

using context = Cratis.Chronicle.Integration.for_Observers.when_removing_an_observer.and_its_client_has_reconnected.context;
using ObserverRemovalResult = Cratis.Chronicle.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Integration.for_Observers.when_removing_an_observer;

/// <summary>
/// A temporary disconnect is not client disposal: querying the owner reconnects and registers a replacement
/// subscription. Management must refuse removal and leave the replacement handling events.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_its_client_has_reconnected(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_independently_owned_reactor(fixture)
    {
        public ObserverRemovalResult Result = default!;
        public ReactorState StateAfterRefusedRemoval = default!;
        public bool IsListedAfterRefusedRemoval;
        public bool HasReplacementHandler;

        async Task Establish()
        {
            var originalHandler = _ownerHandler;
            var originalConnectionId = _ownerStore.Connection.Lifecycle.ConnectionId;
            await _ownerStore.Connection.Lifecycle.Disconnected();
            await _managementHandler.WaitForState(ObserverRunningState.Disconnected, TimeSpanFactory.FromSeconds(30));

            // Reproduce the old client-gone premise explicitly: this query brings the owner back.
            await originalHandler.GetState();
            _ownerStore.Connection.Lifecycle.IsConnected.ShouldBeTrue();
            _ownerStore.Connection.Lifecycle.ConnectionId.ShouldNotEqual(originalConnectionId);
            _ownerHandler = _ownerStore.Reactors.GetHandlerFor<ReactorWithoutDelay>();
            HasReplacementHandler = !ReferenceEquals(originalHandler, _ownerHandler);
            await _ownerHandler.WaitTillSubscribed(TimeSpanFactory.FromSeconds(30));
            await _ownerHandler.WaitTillActive(TimeSpanFactory.FromSeconds(30));
        }

        async Task Because()
        {
            Result = await EventStore.Observers.Remove(ObserverId);
            IsListedAfterRefusedRemoval = await IsListed();

            var appendResult = await EventStore.EventLog.Append("some-source", new SomeEvent(1));
            await _ownerHandler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);
            await Reactor.WaitTillHandledEventReaches(1);
            StateAfterRefusedRemoval = await _managementHandler.WaitTillActiveAndGetState(TimeSpanFactory.FromSeconds(30));
        }
    }

    [Fact] void should_have_replaced_the_subscription_handler() => Context.HasReplacementHandler.ShouldBeTrue();
    [Fact] void should_refuse_the_removal() => Context.Result.IsRemoved.ShouldBeFalse();
    [Fact] void should_report_that_a_client_is_still_subscribed() => Context.Result.Outcome.ShouldEqual(ObserverRemovalOutcome.ObserverSubscribed);
    [Fact] void should_name_the_default_namespace_that_blocked_it() => Context.Result.BlockingNamespace.ShouldEqual(EventStoreNamespaceName.Default.Value);
    [Fact] void should_still_list_the_observer() => Context.IsListedAfterRefusedRemoval.ShouldBeTrue();
    [Fact] void should_leave_the_observer_subscribed() => Context.StateAfterRefusedRemoval.IsSubscribed.ShouldBeTrue();
    [Fact] void should_leave_the_observer_active() => Context.StateAfterRefusedRemoval.RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_leave_the_observer_handling_events() => Context.Reactor.HandledEvents.ShouldEqual(1);
}
