// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_a_batch_has_multiple_conflicts : given.a_batch_with_conditional_conflicts
{
    void Establish()
    {
        _batch = [Request(Before), Request(Conflict), Request(After), Request(OtherConflict)];
        _conflictingIds = [Conflict, OtherConflict];
    }

    Task Because() => RegisterBatch();

    [Fact] void should_report_an_aggregate() => _error.ShouldBeOfExactType<AggregateException>();
    [Fact] void should_report_both_conflicts() => ((AggregateException)_error!).InnerExceptions.Count.ShouldEqual(2);
    [Fact] void should_retain_the_existing_conflict_types() => ((AggregateException)_error!).InnerExceptions.All(_ => _ is EventTypeSchemaChanged).ShouldBeTrue();

    [Theory]
    [InlineData(Conflict)]
    [InlineData(OtherConflict)]
    void should_report_each_conflict_through_the_existing_command_result(string id) =>
        CommandResult.Error(Guid.Empty, _error!).ExceptionMessages.Any(_ => _.Contains(id, StringComparison.Ordinal)).ShouldBeTrue();

    [Fact] async Task should_store_a_success_after_the_first_conflict() => (await _memory.GetDefinition(After)).Generations.Single().Schema.Properties["value"].Type.ShouldEqual(JsonObjectType.String);
    [Fact] async Task should_record_migrations_for_a_success_after_the_first_conflict() => (await _memory.GetMigrationsVersions(After)).Count.ShouldEqual(1);
    [Fact] async Task should_invalidate_a_success_after_the_first_conflict() => await _eventTypesCacheClient.Received(1).Invalidate("example-store", After);

    [Fact]
    async Task should_append_a_system_event_for_a_success_after_the_first_conflict() => await _systemEventSequence.Received(1).Append(
        Arg.Is<EventSourceId>(id => id.Value == After),
        Arg.Is<EventTypeAdded>(@event => @event.EventTypeId.Value == After));

    [Fact]
    async Task should_finalize_only_successful_registrations() => await _eventTypesStorage.Received(1).Register(
        Arg.Is<IEnumerable<EventTypeToRegister>>(types => types.Count() == 2 && types.All(_ => _.Definition.Id.Value == Before || _.Definition.Id.Value == After)));
}
