// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration;

/// <summary>
/// Migrates <see cref="ContractSigned"/> from generation 1 to generation 2.
/// Generation 2 introduced a <c>Status</c> property. Events stored before generation 2
/// receive the default value <c>pending</c>.
/// </summary>
public class ContractSignedMigrator : IEventTypeMigrationFor<ContractSigned>
{
    /// <inheritdoc/>
    public EventTypeGeneration From => 1;

    /// <inheritdoc/>
    public EventTypeGeneration To => 2;

    /// <inheritdoc/>
    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.DefaultValue("status", "pending"));

    /// <inheritdoc/>
    public void Downcast(IEventMigrationBuilder builder)
    {
        // 'status' does not exist in generation 1 — nothing to map back
    }
}
