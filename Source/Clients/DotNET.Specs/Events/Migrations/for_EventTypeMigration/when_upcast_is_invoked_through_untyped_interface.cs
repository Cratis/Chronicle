// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_upcast_is_invoked_through_untyped_interface : Specification
{
    CapturingMigration _migration;
    EventMigrationBuilder _builder;

    void Establish()
    {
        _migration = new CapturingMigration();
        _builder = new EventMigrationBuilder();
    }

    void Because() => ((IEventTypeMigration)_migration).Upcast(_builder);

    [Fact] void should_invoke_typed_upcast() => _migration.UpcastCalled.ShouldBeTrue();

    class CapturingMigration : EventTypeMigration<TestEventV2, TestEventV1>
    {
        public bool UpcastCalled { get; private set; }
        public bool DowncastCalled { get; private set; }

        public override void Upcast(IEventMigrationBuilder<TestEventV2, TestEventV1> builder) => UpcastCalled = true;

        public override void Downcast(IEventMigrationBuilder<TestEventV1, TestEventV2> builder) => DowncastCalled = true;
    }
}
