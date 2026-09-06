// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_previous_generation_uses_the_generation_for_attribute : Specification
{
    ValidMigration _migration;

    void Because() => _migration = new ValidMigration();

    [Fact] void should_resolve_from_generation_1() => _migration.From.Value.ShouldEqual(1u);
    [Fact] void should_resolve_to_generation_2() => _migration.To.Value.ShouldEqual(2u);

    [EventTypeGenerationFor<TestEventV2>(1)]
    record TestEventV1UsingGenerationFor(string FullName);

    class ValidMigration : EventTypeMigration<TestEventV2, TestEventV1UsingGenerationFor>
    {
        public override void Upcast(IEventMigrationBuilder<TestEventV2, TestEventV1UsingGenerationFor> builder) { }

        public override void Downcast(IEventMigrationBuilder<TestEventV1UsingGenerationFor, TestEventV2> builder) { }
    }
}
