// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_event_with_registered_migration;

public class ContractSignedMigrator : IEventTypeMigrationFor<ContractSigned>
{
    public EventTypeGeneration From => 1;

    public EventTypeGeneration To => 2;

    public void Upcast(IEventMigrationBuilder builder) =>
        builder.Properties(pb => pb.DefaultValue("status", "pending"));

    public void Downcast(IEventMigrationBuilder builder)
    {
        // 'status' does not exist in generation 1 — nothing to map back
    }
}
