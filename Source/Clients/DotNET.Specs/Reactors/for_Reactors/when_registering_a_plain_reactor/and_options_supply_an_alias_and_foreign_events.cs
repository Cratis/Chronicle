// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_a_plain_reactor;

public class and_options_supply_an_alias_and_foreign_events : an_explicit_store
{
    IReactorHandler _handler;
    PlainObserver _observer;
    ReactorInvocationResult _invocation;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("event-alias").ForEventStore("foreign"))
            .RegisterReactor<PlainObserver>("observer-alias");
        _observer = new();
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _handler = _store.Reactors.GetHandlerFor<PlainObserver>();
        await using var activated = new ActivatedArtifact(_observer, typeof(PlainObserver), NullLogger<ActivatedArtifact>.Instance);
        var invoker = new ReactorInvoker(_store.EventTypes, Substitute.For<IReactorMiddlewares>(), typeof(PlainObserver), activated, NullLogger<ReactorInvoker>.Instance)
        {
            RegisteredId = _handler.Id
        };
        _invocation = await invoker.Invoke(new PlainEvent("delivered"), EventContext.Empty with { EventType = _store.EventTypes.GetEventTypeFor(typeof(PlainEvent)) });
    }

    [Fact] void should_use_the_explicit_observer_identifier() => _handler.Id.ShouldEqual((ReactorId)"observer-alias");
    [Fact] void should_infer_the_foreign_inbox_from_registry_metadata() => _handler.EventSequenceId.ShouldEqual(new EventSequenceId("inbox-foreign"));
    [Fact] void should_subscribe_with_the_registry_event_identifier() => _handler.EventTypes.ShouldContainOnly(new EventType("event-alias", 1));
    [Fact] void should_invoke_the_plain_instance_method() => _observer.Name.ShouldEqual("delivered");
    [Fact] void should_report_successful_invocation_without_a_marker_cast() => _invocation.IsSuccess.ShouldBeTrue();

    public class PlainObserver
    {
        public string Name { get; private set; } = string.Empty;

        public void Receive(PlainEvent @event) => Name = @event.Name;
    }
}
