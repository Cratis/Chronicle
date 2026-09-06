// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigrators.when_getting_migrators_for;

public class and_two_migrators_cover_the_same_generations : given.all_dependencies
{
    EventTypeMigrators _migrators;
    Exception _error;

    void Establish()
    {
        _clientArtifactsProvider.EventTypeMigrators.Returns([typeof(FirstMigrator), typeof(SecondMigrator)]);
        _migrators = new EventTypeMigrators(_clientArtifactsProvider, _serviceProvider);
    }

    void Because() => _error = Catch.Exception(() => _migrators.GetMigratorsFor(typeof(TestEvent)));

    [Fact] void should_throw_multiple_migrators_for_same_event_type_generation() => _error.ShouldBeOfExactType<MultipleMigratorsForSameEventTypeGeneration>();
    [Fact] void should_identify_the_target_event_type_by_its_explicit_id() =>
        ((MultipleMigratorsForSameEventTypeGeneration)_error).EventType.Id.Value.ShouldEqual("my-test-event");

    [EventType("my-test-event")]
    class TestEvent;

    class FirstMigrator : IEventTypeMigrationFor<TestEvent>
    {
        public EventTypeGeneration From => 1;
        public EventTypeGeneration To => 2;
        public void Upcast(IEventMigrationBuilder builder) { }
        public void Downcast(IEventMigrationBuilder builder) { }
    }

    class SecondMigrator : IEventTypeMigrationFor<TestEvent>
    {
        public EventTypeGeneration From => 1;
        public EventTypeGeneration To => 2;
        public void Upcast(IEventMigrationBuilder builder) { }
        public void Downcast(IEventMigrationBuilder builder) { }
    }
}
