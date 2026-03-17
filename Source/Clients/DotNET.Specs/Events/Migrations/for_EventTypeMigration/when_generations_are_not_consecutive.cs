// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_generations_are_not_consecutive : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() =>
    {
        _ = new InvalidMigration();
        return Task.CompletedTask;
    });

    [Fact] void should_throw_invalid_migration_generation_gap() => _error.ShouldBeOfExactType<InvalidMigrationGenerationGap>();

    class InvalidMigration : EventTypeMigration<TestEventV5, TestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<TestEventV5, TestEventV1> builder) { }

        public override void Downcast(IEventMigrationBuilder<TestEventV1, TestEventV5> builder) { }
    }
}
