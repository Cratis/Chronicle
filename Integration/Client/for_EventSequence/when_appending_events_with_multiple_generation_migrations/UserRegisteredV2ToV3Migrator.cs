// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

public class UserRegisteredV2ToV3Migrator : EventTypeMigration<UserRegisteredV3, UserRegisteredV2>
{
    public override void Upcast(IEventMigrationBuilder<UserRegisteredV3, UserRegisteredV2> builder) =>
        builder.Properties(pb => pb.DefaultValue(t => t.Email, "unknown@example.com"));

    public override void Downcast(IEventMigrationBuilder<UserRegisteredV2, UserRegisteredV3> builder)
    {
        // Email does not exist in generation 2 — nothing to map back.
    }
}
