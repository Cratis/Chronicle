// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_checking;

public class and_event_type_has_no_migrators : given.all_dependencies
{
    IReadOnlyList<EventTypeCheckResult> _results;

    async Task Because() => _results = await _subject.Check(
        "test-store",
        [new EventTypeRegistration { Type = new() { Id = "some-event", Generation = 2 }, Schema = "{}" }],
        _storage);

    [Fact] void should_reject_it() => _results.Single().Accepted.ShouldBeFalse();
    [Fact] void should_give_the_reason_registering_throws() => _results.Single().Reason.ShouldEqual(new MissingEventTypeMigrators("some-event", 2).Message);
    [Fact] void should_name_the_generation() => _results.Single().Generation.ShouldEqual(2u);
}
