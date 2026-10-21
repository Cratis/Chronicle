// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.when_a_namespace_is_created_after_registration.and_it_is_explicitly_ensured.context;
using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;
using KernelEventSequenceNumber = Cratis.Chronicle.Concepts.Events.EventSequenceNumber;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.when_a_namespace_is_created_after_registration;

[Collection(given.NamespaceSubscriptionCollection.Name)]
public class and_it_is_explicitly_ensured(context context) : Given<context>(context)
{
    public class context(given.NamespaceSubscriptionFixture fixture) : given.a_registered_store(fixture)
    {
        public bool SubscribedOnReturn;
        public bool HandledFirstEvent;

        async Task Because()
        {
            await new EnsureNamespace(_storeName, _newTenant).Handle(_grains);
            SubscribedOnReturn = await _grains.GetGrain<IObserver>(_observerKey).IsSubscribed();
            var store = await ChronicleClient.GetEventStore(_storeName.Value, _newTenant.Value);
            var append = await store.EventLog.Append("customer", new CustomerNamed("Explicit tenant"));
            append.IsSuccess.ShouldBeTrue();
            HandledFirstEvent = await _observerStorage.WaitForHandled(_observerKey, (KernelEventSequenceNumber)append.SequenceNumber.Value) == append.SequenceNumber.Value;
        }
    }

    [Fact] void should_return_with_the_projection_subscribed() => Context.SubscribedOnReturn.ShouldBeTrue();
    [Fact] void should_handle_the_first_append_without_reregistering_the_projection() => Context.HandledFirstEvent.ShouldBeTrue();
}
