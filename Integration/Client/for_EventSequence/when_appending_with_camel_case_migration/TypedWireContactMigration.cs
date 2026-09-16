// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

public class TypedWireContactMigration : EventTypeMigration<WireContactRecorded, WireContactRecordedV1>
{
    public override void Upcast(IEventMigrationBuilder<WireContactRecorded, WireContactRecordedV1> builder) => builder.Properties(properties => properties
        .RenamedFrom(target => target.Contact.Email, source => source.Contact.EmailAddress)
        .RenamedFrom(target => target.Contact.Phone, source => source.Contact.PhoneNumber));

    public override void Downcast(IEventMigrationBuilder<WireContactRecordedV1, WireContactRecorded> builder) => builder.Properties(properties => properties
        .RenamedFrom(target => target.Contact.EmailAddress, source => source.Contact.Email)
        .RenamedFrom(target => target.Contact.PhoneNumber, source => source.Contact.Phone));
}
