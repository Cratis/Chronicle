// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.when_a_namespace_is_created_after_registration.and_the_first_append_creates_it.context;
using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;
using KernelEventSequenceNumber = Cratis.Chronicle.Concepts.Events.EventSequenceNumber;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.when_a_namespace_is_created_after_registration;

/// <summary>
/// Covers implicit namespace creation end to end, including the namespace-added broadcast and event-log timer.
/// The timer path independently of the broadcast is pinned by the for_EventSequence unit specifications.
/// </summary>
/// <param name="context">The specification context.</param>
[Collection(given.NamespaceSubscriptionCollection.Name)]
public class and_the_first_append_creates_it(context context) : Given<context>(context)
{
    public class context(given.NamespaceSubscriptionFixture fixture) : given.a_registered_store(fixture)
    {
        public bool Subscribed;
        public bool HandledFirstEvent;

        async Task Because()
        {
            // No client registration or explicit namespace command in this tenant: the first append creates it,
            // and broadcast plus timer reconciliation arrange subscriptions and catch-up from First.
            var store = await ChronicleClient.GetEventStore(_storeName.Value, _newTenant.Value);
            var append = await store.EventLog.Append("customer", new CustomerNamed("Later tenant"));
            append.IsSuccess.ShouldBeTrue();
            HandledFirstEvent = await _observerStorage.WaitForHandled(_observerKey, (KernelEventSequenceNumber)append.SequenceNumber.Value) == append.SequenceNumber.Value;
            Subscribed = await _grains.GetGrain<IObserver>(_observerKey).IsSubscribed();
        }
    }

    [Fact] void should_subscribe_the_already_registered_projection() => Context.Subscribed.ShouldBeTrue();
    [Fact] void should_catch_up_the_first_event_in_the_new_namespace() => Context.HandledFirstEvent.ShouldBeTrue();
}
