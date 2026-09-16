// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

public class ContactRecordedMigration : EventTypeMigration<ContactRecorded, ContactRecordedV1>
{
    public override void Upcast(IEventMigrationBuilder<ContactRecorded, ContactRecordedV1> builder) => builder.Properties(properties => properties
        .RenamedFrom(target => target.Contact.Email, source => source.Contact.EmailAddress)
        .DefaultValue(target => target.Status, "active"));

    public override void Downcast(IEventMigrationBuilder<ContactRecordedV1, ContactRecorded> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.Contact.EmailAddress, source => source.Contact.Email));
}
