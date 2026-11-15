// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_checking;

public class and_the_input_would_write_something : given.a_store_to_leave_unchanged
{
    string _before;
    string _after;
    IReadOnlyList<EventTypeCheckResult> _results;

    async Task Because()
    {
        _before = await Snapshot();
        _results = await _subject.Check("test-store", [_request, _newEventType, _newGeneration, _rejected], _storage);
        _after = await Snapshot();
    }

    [Fact] void should_have_something_stored() => _before.ShouldNotBeEmpty();
    [Fact] void should_accept_the_new_event_type() => _results[1].Accepted.ShouldBeTrue();
    [Fact] void should_accept_the_new_generation() => _results[2].Accepted.ShouldBeTrue();
    [Fact] void should_reject_the_changed_schema() => _results[3].Accepted.ShouldBeFalse();
    [Fact] void should_leave_the_stored_definitions_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_not_register_anything() => _eventTypesStorage.DidNotReceive().Register(Arg.Any<IEnumerable<Concepts.Events.EventTypeToRegister>>());
    [Fact] void should_not_record_a_migrations_version() => _eventTypesStorage.DidNotReceive().RecordMigrationsVersion(Arg.Any<Concepts.Events.EventTypeId>(), Arg.Any<Concepts.Events.EventTypeMigrationsVersion>(), Arg.Any<IEnumerable<Concepts.Events.EventTypeMigrationDefinition>>());
    [Fact] void should_not_evict_the_cache() => _eventTypesCacheClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_capture_patterns() => _patternCapture.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_touch_the_system_event_sequence() => _systemEventSequence.ReceivedCalls().ShouldBeEmpty();
}
