// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation;

public class OrderPlacedMigrator : IEventTypeMigrationFor<OrderPlaced>
{
    public EventTypeGeneration From => 1;

    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.DefaultValue("Status", "pending"));

    public void Downcast(IEventMigrationBuilder builder)
    {
        // Status doesn't exist in gen 1, nothing to map back.
    }
}
