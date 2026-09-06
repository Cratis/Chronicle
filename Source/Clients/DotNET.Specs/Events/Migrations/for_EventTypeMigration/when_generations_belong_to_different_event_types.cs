// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_generations_belong_to_different_event_types : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() =>
    {
        _ = new InvalidMigration();
        return Task.CompletedTask;
    });

    [Fact] void should_throw_migration_generations_must_share_event_type_id() => _error.ShouldBeOfExactType<MigrationGenerationsMustShareEventTypeId>();

    [EventType("OtherEvent", generation: 2)]
    record OtherEventV2(string Name);

    class InvalidMigration : EventTypeMigration<OtherEventV2, TestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<OtherEventV2, TestEventV1> builder) { }

        public override void Downcast(IEventMigrationBuilder<TestEventV1, OtherEventV2> builder) { }
    }
}
