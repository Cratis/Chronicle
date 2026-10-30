// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_resolving_before_registration;

public class and_registration_has_run_afterwards : given.an_event_types_without_registration
{
    IEventTypesStorage _eventTypesStorage;

    void Establish()
    {
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _storage.GetEventStore(EventStoreName.System).EventTypes.Returns(_eventTypesStorage);
    }

    async Task Because()
    {
        _eventTypes.GetClrTypeFor(typeof(AlertRaised).GetEventType().Id);
        _eventTypes.GetJsonSchema(typeof(AlertRaised));
        await _eventTypes.DiscoverAndRegister(EventStoreName.System);
    }

    [Fact] void should_still_resolve_the_clr_type() => _eventTypes.GetClrTypeFor(typeof(AlertRaised).GetEventType().Id).ShouldEqual(typeof(AlertRaised));
    [Fact] async Task should_register_alert_raised() => await ShouldRegister(typeof(AlertRaised));
    [Fact] async Task should_register_alert_escalated() => await ShouldRegister(typeof(AlertEscalated));
    [Fact] async Task should_register_alert_cleared() => await ShouldRegister(typeof(AlertCleared));

    async Task ShouldRegister(Type type) => await _eventTypesStorage.Received(1).Register(
        type.GetEventType(),
        Arg.Any<JsonSchema>(),
        EventTypeOwner.Server,
        EventTypeSource.Code);
}
