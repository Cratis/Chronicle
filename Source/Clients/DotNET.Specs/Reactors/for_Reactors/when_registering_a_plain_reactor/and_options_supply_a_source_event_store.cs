// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_a_plain_reactor;

public class and_options_supply_a_source_event_store : an_explicit_store
{
    readonly string _hostStore = "host-product";
    IReactorHandler _handler;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>()
            .RegisterReactor<PlainObserver>(ReactorRegistrationOptions.FromEventStore(_hostStore));
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _handler = _store.Reactors.GetHandlerFor<PlainObserver>();
    }

    [Fact] void should_register_the_configured_inbox() => _handler.EventSequenceId.ShouldEqual(new EventSequenceId("inbox-host-product"));

    public class PlainObserver
    {
        public void Receive(PlainEvent @event) { }
    }
}
