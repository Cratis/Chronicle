// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations.for_EventTypeMigration.given;

public class ModuleAssignedV1ToV2 : EventTypeMigration<ModuleAssigned, ModuleAssignedV1>
{
    public override void Upcast(IEventMigrationBuilder<ModuleAssigned, ModuleAssignedV1> builder) =>
        builder.Properties(properties => properties.DefaultValue(topic => topic.Module, new ModuleKey("global")));

    public override void Downcast(IEventMigrationBuilder<ModuleAssignedV1, ModuleAssigned> builder) =>
        builder.Properties(_ => { });
}
