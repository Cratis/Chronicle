// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using context = Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_dormant_observer_is_woken.and_its_definition_is_read.context;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.when_a_dormant_observer_is_woken;

// Waking a dormant observer leaves its event-store-level definition as it was (#6110).
[Collection(given.DormantSafetyNetCollection.Name)]
public class and_its_definition_is_read(context context) : Given<context>(context)
{
    public class context(given.DormantSafetyNetFixture fixture) : given.two_dormant_observers(fixture)
    {
        public ObserverDefinition Before = ObserverDefinition.Empty;
        public ObserverDefinition After = ObserverDefinition.Empty;
        public ObserverDefinition OtherObserver = ObserverDefinition.Empty;

        async Task Because()
        {
            var observers = Services.GetRequiredService<IStorage>().GetEventStore(_store.Name.Value).Observers;
            Before = await observers.Get(_alpha.Id.Value);
            await _store.EventLog.Append("alpha", new given.DormantAlphaEvent("second"));
            await WaitForHandled(Alpha.Handled, 2);
            await WaitUntilReleased(_alpha);
            After = await observers.Get(_alpha.Id.Value);
            OtherObserver = await observers.Get(_beta.Id.Value);
        }
    }

    [Fact] void should_have_a_definition_for_the_observer() => Context.Before.EventTypes.ShouldNotBeEmpty();
    [Fact] void should_keep_the_event_types_over_the_wake() => Context.After.EventTypes.ShouldEqual(Context.Before.EventTypes);
    [Fact] void should_keep_the_owner_over_the_wake() => Context.After.Owner.ShouldEqual(Context.Before.Owner);
    [Fact] void should_keep_the_event_sequence_over_the_wake() => Context.After.EventSequenceId.ShouldEqual(Context.Before.EventSequenceId);
    [Fact] void should_keep_the_definition_of_the_other_observer_apart() => (Context.OtherObserver.Identifier != Context.After.Identifier).ShouldBeTrue();
}
