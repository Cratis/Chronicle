// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation;

/// <summary>
/// Migrates OrderPlaced between generations 1 and 2.
/// Generation 1 has only Description.
/// Generation 2 adds a Status field with a default value.
/// </summary>
public class OrderPlacedMigrator : IEventTypeMigrationFor<OrderPlaced>
{
    /// <inheritdoc/>
    public EventTypeGeneration From => 1;

    /// <inheritdoc/>
    public EventTypeGeneration To => 2;

    /// <inheritdoc/>
    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.DefaultValue("Status", "pending"));

    /// <inheritdoc/>
    public void Downcast(IEventMigrationBuilder builder)
    {
        // Status doesn't exist in gen 1, nothing to map back.
    }
}
