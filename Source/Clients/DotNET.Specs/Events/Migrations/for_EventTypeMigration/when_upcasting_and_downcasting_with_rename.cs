// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration;

public class when_upcasting_and_downcasting_with_rename : Specification
{
    RenameMigration _migration;
    EventMigrationBuilder _upcastBuilder;
    EventMigrationBuilder _downcastBuilder;
    JsonObject _upcastResult;
    JsonObject _downcastResult;

    void Establish()
    {
        _migration = new RenameMigration();
        _upcastBuilder = new EventMigrationBuilder();
        _downcastBuilder = new EventMigrationBuilder();
    }

    void Because()
    {
        ((IEventTypeMigration)_migration).Upcast(_upcastBuilder);
        ((IEventTypeMigration)_migration).Downcast(_downcastBuilder);
        _upcastResult = _upcastBuilder.ToJson();
        _downcastResult = _downcastBuilder.ToJson();
    }

    [Fact] void should_upcast_with_rename_expression() => _upcastResult["Email"].ToString().ShouldContain("$rename");
    [Fact] void should_upcast_from_old_property_name() => _upcastResult["Email"]["$rename"].GetValue<string>().ShouldEqual("EmailAddress");
    [Fact] void should_downcast_with_rename_expression() => _downcastResult["EmailAddress"].ToString().ShouldContain("$rename");
    [Fact] void should_downcast_from_new_property_name() => _downcastResult["EmailAddress"]["$rename"].GetValue<string>().ShouldEqual("Email");

    class RenameMigration : EventTypeMigration<RenameTestEventV2, RenameTestEventV1>
    {
        public override void Upcast(IEventMigrationBuilder<RenameTestEventV2, RenameTestEventV1> builder) =>
            builder.Properties(pb => pb
                .RenamedFrom(t => t.Email, s => s.EmailAddress));

        public override void Downcast(IEventMigrationBuilder<RenameTestEventV1, RenameTestEventV2> builder) =>
            builder.Properties(pb => pb
                .RenamedFrom(t => t.EmailAddress, s => s.Email));
    }
}
