// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Observers.when_removing_an_observer.and_its_client_is_gone.context;

// The kernel's Core assembly declares its own ObserverRemovalResult, and this project references both it and the
// client. The client's is the one a consumer sees, so it is the one specified here.
using ObserverRemovalResult = Cratis.Chronicle.Observation.ObserverRemovalResult;

namespace Cratis.Chronicle.Integration.for_Observers.when_removing_an_observer;

/// <summary>
/// The case the whole operation exists for: an observer that was registered, whose declaring code is no longer
/// reporting it, and whose records would otherwise sit in the store forever. Removing it has to actually clear the
/// listing - a removal that reports success while the observer is still enumerated is the bug rather than the fix.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class and_its_client_is_gone(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.an_independently_owned_reactor(chronicleFixture)
    {
        public ReactorState StateBeforeRemoval = default!;
        public ObserverRemovalResult Result = default!;
        public bool WasListedBeforeRemoval;
        public bool IsListedAfterRemoval;

        async Task Because()
        {
            WasListedBeforeRemoval = await IsListed();

            // Dispose the independently owned client and its subscription. Query only through management afterward:
            // a lifecycle disconnect followed by a query through the owner reconnects and re-registers its reactor.
            DisposeOwner();
            StateBeforeRemoval = await _managementHandler.WaitForStateAndGetState(ObserverRunningState.Disconnected, TimeSpanFactory.FromSeconds(30));
            StateBeforeRemoval.IsSubscribed.ShouldBeFalse();

            Result = await EventStore.Observers.Remove(ObserverId);
            IsListedAfterRemoval = await IsListed();
        }
    }

    [Fact] void should_have_listed_the_observer_before_removing_it() => Context.WasListedBeforeRemoval.ShouldBeTrue();

    [Fact] void should_have_ended_the_subscription() => Context.StateBeforeRemoval.IsSubscribed.ShouldBeFalse();

    [Fact] void should_remove_the_observer() => Context.Result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);

    [Fact] void should_not_name_a_blocking_namespace() => Context.Result.BlockingNamespace.ShouldBeEmpty();

    [Fact] void should_no_longer_list_the_observer() => Context.IsListedAfterRemoval.ShouldBeFalse();
}
