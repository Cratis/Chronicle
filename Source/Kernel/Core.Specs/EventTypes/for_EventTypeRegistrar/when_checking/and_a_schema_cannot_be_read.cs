// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_checking;

public class and_a_schema_cannot_be_read : given.all_dependencies
{
    const string ValidSchema = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    IReadOnlyList<EventTypeCheckResult> _results;

    async Task Because() => _results = await _subject.Check(
        "test-store",
        [
            new EventTypeRegistration { Type = new() { Id = "broken-event", Generation = 1 }, Schema = "{ this is not a schema" },
            new EventTypeRegistration { Type = new() { Id = "fine-event", Generation = 1 }, Schema = ValidSchema }
        ],
        _storage);

    [Fact] void should_report_every_registration() => _results.Count.ShouldEqual(2);
    [Fact] void should_reject_the_unreadable_one() => _results[0].Accepted.ShouldBeFalse();
    [Fact] void should_name_the_unreadable_event_type() => _results[0].EventType.ShouldEqual("broken-event");
    [Fact] void should_give_a_reason() => _results[0].Reason.ShouldContain("could not be processed");
    [Fact] void should_still_accept_the_other_one() => _results[1].Accepted.ShouldBeTrue();
}
