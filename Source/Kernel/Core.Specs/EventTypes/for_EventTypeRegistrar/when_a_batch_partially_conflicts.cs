// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar;

public class when_a_batch_partially_conflicts : given.a_batch_with_conditional_conflicts
{
    void Establish()
    {
        _batch = [Request(Before), Request(Conflict)];
        _conflictingIds = [Conflict];
    }

    Task Because() => RegisterBatch();

    [Fact] void should_report_the_conflict() => _error.ShouldBeOfExactType<EventTypeSchemaChanged>();
    [Fact] void should_preserve_the_single_conflict_message() => _error!.Message.ShouldEqual(new EventTypeSchemaChanged(Conflict, 1).Message);
    [Fact] async Task should_store_the_successful_schema() => (await _memory.GetDefinition(Before)).Generations.Single().Schema.Properties["value"].Type.ShouldEqual(JsonObjectType.String);
    [Fact] async Task should_record_the_successful_migrations_version() => (await _memory.GetMigrationsVersions(Before)).Count.ShouldEqual(1);
    [Fact] async Task should_invalidate_the_successful_type() => await _eventTypesCacheClient.Received(1).Invalidate("example-store", Before);
    [Fact] async Task should_update_pattern_capture() => await _patternCapture.Received(1).SubscribeAcrossNamespaces("example-store");

    [Fact]
    async Task should_append_the_successful_type_system_event() => await _systemEventSequence.Received(1).Append(
        Arg.Is<EventSourceId>(id => id.Value == Before),
        Arg.Is<EventTypeAdded>(@event => @event.EventTypeId.Value == Before));

    [Fact] async Task should_preserve_the_conflicting_winners_schema() => (await _memory.GetDefinition(Conflict)).Generations.Single().Schema.Properties["value"].Type.ShouldEqual(JsonObjectType.Integer);
    [Fact] void should_not_append_a_system_event_for_the_conflict() => _systemEventSequence.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == "Append").ShouldEqual(1);
    [Fact] async Task should_not_invalidate_the_conflicting_type() => await _eventTypesCacheClient.DidNotReceive().Invalidate("example-store", Conflict);

    [Fact]
    async Task should_not_register_the_conflicting_types_remaining_metadata() => await _eventTypesStorage.Received(1).Register(
        Arg.Is<IEnumerable<EventTypeToRegister>>(types => types.All(_ => _.Definition.Id.Value == Before)));
}
