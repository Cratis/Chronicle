// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Compliance.for_EventComplianceHelper.when_decrypting_events;

public class and_event_has_no_subject : given.all_dependencies
{
    AppendedEvent _event;
    AppendedEvent[] _result;

    void Establish()
    {
        _event = new AppendedEvent(
            EventContext.Empty with { EventType = SomeEventType, Subject = null! },
            new ExpandoObject());
    }

    async Task Because() => _result = await _helper.DecryptEvents(
        [_event],
        new Dictionary<EventType, EventTypeSchema> { { SomeEventType, new EventTypeSchema(SomeEventType, EventTypeOwner.Client, EventTypeSource.Code, _schemaWithPii) } });

    [Fact] void should_return_the_event_unchanged() => _result[0].ShouldEqual(_event);
    [Fact] void should_not_call_compliance_manager() => _complianceManager.DidNotReceive().Release(default!, default!, default!, default!, default!);
}
