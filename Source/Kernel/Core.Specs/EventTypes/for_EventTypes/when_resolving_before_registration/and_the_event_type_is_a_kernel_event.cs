// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_resolving_before_registration;

public class and_the_event_type_is_a_kernel_event : given.an_event_types_without_registration
{
    Type _type;
    JsonSchema _schema;

    void Because()
    {
        _type = _eventTypes.GetClrTypeFor(typeof(AlertRaised).GetEventType().Id);
        _schema = _eventTypes.GetJsonSchema(_type);
    }

    [Fact] void should_resolve_the_clr_type() => _type.ShouldEqual(typeof(AlertRaised));
    [Fact] void should_provide_a_schema() => _schema.ShouldNotBeNull();
    [Fact] void should_not_touch_storage() => _storage.DidNotReceive().GetEventStore(Arg.Any<EventStoreName>());
}
