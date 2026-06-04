// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Compliance.for_EventCompliance.when_decrypting_events;

public class and_event_type_is_not_in_schema_map : given.all_dependencies
{
    AppendedEvent _event;
    AppendedEvent[] _result;

    void Establish()
    {
        _event = AppendedEvent.EmptyWithEventType(OtherEventType);
    }

    async Task Because() => _result = await _compliance.DecryptEvents(
        [_event],
        new Dictionary<EventType, EventTypeSchema> { { SomeEventType, new EventTypeSchema(SomeEventType, EventTypeOwner.Client, EventTypeSource.Code, _schemaWithPii) } });

    [Fact] void should_return_the_event_unchanged() => _result[0].ShouldEqual(_event);
    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Release(default!, default!, default!, default!, default!);
}
