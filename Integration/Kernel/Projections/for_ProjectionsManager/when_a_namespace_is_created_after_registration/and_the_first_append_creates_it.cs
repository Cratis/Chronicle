// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.when_a_namespace_is_created_after_registration.and_the_first_append_creates_it.context;
using CustomerNamed = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given.CustomerNamed;
using KernelEventSequenceId = Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelEventSequenceKey = Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionsManager.when_a_namespace_is_created_after_registration;

[Collection(given.NamespaceSubscriptionCollection.Name)]
public class and_the_first_append_creates_it(context context) : Given<context>(context)
{
    public class context(given.NamespaceSubscriptionFixture fixture) : given.a_registered_store(fixture)
    {
        public bool Subscribed;
        public bool HandledFirstEvent;

        async Task Because()
        {
            // No client registration or explicit namespace command in this tenant: activating the log
            // for its first append must arrange subscriptions and let observers catch up from First.
            var log = _grains.GetGrain<IEventSequence>(new KernelEventSequenceKey(KernelEventSequenceId.Log, _storeName, _newTenant));
            var append = await log.Append("customer", new CustomerNamed("Later tenant"));
            append.IsSuccess.ShouldBeTrue();
            HandledFirstEvent = await _observerStorage.WaitForHandled(_observerKey, append.SequenceNumber) == append.SequenceNumber;
            Subscribed = await _grains.GetGrain<IObserver>(_observerKey).IsSubscribed();
        }
    }

    [Fact] void should_subscribe_the_already_registered_projection() => Context.Subscribed.ShouldBeTrue();
    [Fact] void should_catch_up_the_first_event_in_the_new_namespace() => Context.HandledFirstEvent.ShouldBeTrue();
}
