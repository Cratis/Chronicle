// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_generation_is_pinned;

public class SubscriptionPersonMigration : EventTypeMigration<SubscriptionPersonRegistered, SubscriptionPersonRegisteredV1>
{
    public override void Upcast(IEventMigrationBuilder<SubscriptionPersonRegistered, SubscriptionPersonRegisteredV1> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.FullName, source => source.Name));
    public override void Downcast(IEventMigrationBuilder<SubscriptionPersonRegisteredV1, SubscriptionPersonRegistered> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.Name, source => source.FullName));
}
