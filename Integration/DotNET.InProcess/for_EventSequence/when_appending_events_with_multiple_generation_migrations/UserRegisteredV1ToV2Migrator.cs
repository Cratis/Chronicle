// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

public class UserRegisteredV1ToV2Migrator : EventTypeMigration<UserRegisteredV2, UserRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<UserRegisteredV2, UserRegisteredV1> builder) =>
        builder.Properties(pb => pb
            .Split(t => t.FirstName, s => s.FullName, " ", 0)
            .Split(t => t.LastName, s => s.FullName, " ", 1));

    public override void Downcast(IEventMigrationBuilder<UserRegisteredV1, UserRegisteredV2> builder) =>
        builder.Properties(pb => pb.Combine(t => t.FullName, " ", s => s.FirstName, s => s.LastName));
}
