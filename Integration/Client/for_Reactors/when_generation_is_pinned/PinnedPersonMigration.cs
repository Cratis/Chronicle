// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_Reactors.when_generation_is_pinned;

public class PinnedPersonMigration : EventTypeMigration<PinnedPersonRegistered, PinnedPersonRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<PinnedPersonRegistered, PinnedPersonRegisteredV1> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.FullName, source => source.Name));

    public override void Downcast(IEventMigrationBuilder<PinnedPersonRegisteredV1, PinnedPersonRegistered> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.Name, source => source.FullName));
}
