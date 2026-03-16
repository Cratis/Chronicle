// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_generations_are_consecutive : Specification
{
    TestMigration _migration;

    void Because() => _migration = new TestMigration();

    [Fact] void should_set_from_to_previous_generation() => _migration.From.Value.ShouldEqual(1u);
    [Fact] void should_set_to_to_upgrade_generation() => _migration.To.Value.ShouldEqual(2u);

    class TestMigration : EventTypeMigration<TestEventV2, TestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<TestEventV2, TestEventV1> builder) { }

        public override void Downcast(IEventMigrationBuilder<TestEventV1, TestEventV2> builder) { }
    }
}
