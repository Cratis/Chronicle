// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

public class ObjectDefaultMigration : EventTypeMigration<ObjectDefaultRecorded, ObjectDefaultRecordedV1>
{
    public override void Upcast(IEventMigrationBuilder<ObjectDefaultRecorded, ObjectDefaultRecordedV1> builder) => builder.Properties(properties => properties
        .DefaultValue(target => target.Defaults, new ObjectDefaults("global")));

    public override void Downcast(IEventMigrationBuilder<ObjectDefaultRecordedV1, ObjectDefaultRecorded> builder)
    {
    }
}
