// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_registered_migration;

public class EmployeeRegisteredMigrator : IEventTypeMigrationFor<EmployeeRegistered>
{
    public EventTypeGeneration From => 1;

    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            pb.Split("FirstName", "FullName", " ", 0);
            pb.Split("LastName", "FullName", " ", 1);
        });

    public void Downcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.Combine("FullName", " ", "FirstName", "LastName"));
}
