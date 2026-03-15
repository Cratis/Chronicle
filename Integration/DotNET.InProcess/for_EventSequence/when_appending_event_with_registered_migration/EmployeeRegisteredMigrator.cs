// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration;

/// <summary>
/// Migrates <see cref="EmployeeRegistered"/> between generations 1 and 2.
/// Generation 1 had a single FullName property.
/// Generation 2 (current) has separate FirstName and LastName properties.
/// </summary>
public class EmployeeRegisteredMigrator : IEventTypeMigrationFor<EmployeeRegistered>
{
    /// <inheritdoc/>
    public EventTypeGeneration From => 1;

    /// <inheritdoc/>
    public EventTypeGeneration To => 2;

    /// <inheritdoc/>
    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb =>
        {
            pb.Split("FirstName", "FullName", " ", 0);
            pb.Split("LastName", "FullName", " ", 1);
        });

    /// <inheritdoc/>
    public void Downcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.Combine("FullName", "FirstName", "LastName"));
}
