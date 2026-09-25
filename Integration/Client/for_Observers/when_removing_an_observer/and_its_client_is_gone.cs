// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.for_Reactors;
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
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public ReactorWithoutDelay Reactor = default!;
        public ObserverId ObserverId = default!;
        public ObserverRemovalResult Result = default!;
        public bool WasListedBeforeRemoval;
        public bool IsListedAfterRemoval;

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
            ObserverId = reactor.Id.Value;

            WasListedBeforeRemoval = await IsListed();

            // Stands in for the application that declared the reactor being shut down and never coming back. The
            // kernel sees the subscription end; nothing re-declares the observer, so its records are all that is left.
            await EventStore.Connection.Lifecycle.Disconnected();
            await EventStore.Reactors.WaitForState<ReactorWithoutDelay>(ObserverRunningState.Disconnected, TimeSpanFactory.FromSeconds(30));

            Result = await EventStore.Observers.Remove(ObserverId);
            IsListedAfterRemoval = await IsListed();
        }

        async Task<bool> IsListed()
        {
            var observers = await EventStore.Observers.GetAll();
            return observers.Any(observer => observer.Id == ObserverId);
        }
    }

    [Fact] void should_have_listed_the_observer_before_removing_it() => Context.WasListedBeforeRemoval.ShouldBeTrue();

    [Fact] void should_remove_the_observer() => Context.Result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);

    [Fact] void should_not_name_a_blocking_namespace() => Context.Result.BlockingNamespace.ShouldBeEmpty();

    [Fact] void should_no_longer_list_the_observer() => Context.IsListedAfterRemoval.ShouldBeFalse();
}
