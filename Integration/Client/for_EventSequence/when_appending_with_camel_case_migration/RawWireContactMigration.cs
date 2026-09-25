// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_with_camel_case_migration;

public class RawWireContactMigration : IEventTypeMigrationFor<WireContactRecorded>
{
    public EventTypeGeneration From => 1;
    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder) => builder.Properties(properties =>
    {
        properties.RenamedFrom("ContactDetails.WireEmail", "ContactDetails.WireAddress");
        properties.RenamedFrom("ContactDetails.mapped_Phone", "ContactDetails.mapped_PhoneNumber");
    });

    public void Downcast(IEventMigrationBuilder builder) => builder.Properties(properties =>
    {
        properties.RenamedFrom("ContactDetails.WireAddress", "ContactDetails.WireEmail");
        properties.RenamedFrom("ContactDetails.mapped_PhoneNumber", "ContactDetails.mapped_Phone");
    });
}
