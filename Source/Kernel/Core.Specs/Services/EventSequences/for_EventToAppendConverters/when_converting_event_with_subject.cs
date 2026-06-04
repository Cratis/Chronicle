// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Events;
using ContractEventType = Cratis.Chronicle.Contracts.Events.EventType;
using KernelEventToAppend = Cratis.Chronicle.EventSequences.EventToAppend;
using KernelSubject = Cratis.Chronicle.Concepts.Events.Subject;

namespace Cratis.Chronicle.Services.EventSequences.for_EventToAppendConverters;

public class when_converting_event_with_subject : Specification
{
    const string Subject = "person-42";

    KernelEventToAppend _result;

    void Because()
    {
        var eventToAppend = new EventToAppend
        {
            EventSourceId = "request-1",
            EventType = new ContractEventType
            {
                Id = "PersonDetailsRegistered",
                Generation = 1
            },
            Content = """{"name":"Jane"}""",
            Tags = [],
            Subject = Subject
        };

        _result = eventToAppend.ToChronicle(new JsonSerializerOptions());
    }

    [Fact] void should_preserve_subject() => _result.Subject.ShouldEqual(new KernelSubject(Subject));
}
