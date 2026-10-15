// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_migrating_a_concept_default.given;

/// <summary>
/// Assigns the default module to topics created before modules existed.
/// </summary>
public class ModuleTopicCreatedV1ToV2 : EventTypeMigration<ModuleTopicCreated, ModuleTopicCreatedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<ModuleTopicCreated, ModuleTopicCreatedV1> builder) =>
        builder.Properties(properties => properties.DefaultValue<ModuleKey>(topic => topic.Module, new ModuleKey("global")));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<ModuleTopicCreatedV1, ModuleTopicCreated> builder) =>
        builder.Properties(_ => { });
}
