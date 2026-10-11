// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Assigns the original module to topics that predate module tracking.
/// </summary>
public class MigrationTopicCreatedV1ToV2 : EventTypeMigration<MigrationTopicCreated, MigrationTopicCreatedV1>
{
    /// <inheritdoc/>
    public override void Upcast(IEventMigrationBuilder<MigrationTopicCreated, MigrationTopicCreatedV1> builder) =>
        builder.Properties(properties => properties.DefaultValue<string>(topic => topic.Module, "global"));

    /// <inheritdoc/>
    public override void Downcast(IEventMigrationBuilder<MigrationTopicCreatedV1, MigrationTopicCreated> builder) =>
        builder.Properties(_ => { });
}
