// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_a_value_map;

/// <summary>
/// States what the upstream system's old state numbers became, once. Neither direction declares a transformation of
/// its own - the map is read forward when upcasting and inverted when downcasting.
/// </summary>
public class SubscriptionStateChangedMigrator : EventTypeMigration<SubscriptionStateChanged, SubscriptionStateChangedV1>
{
    public override void Upcast(IEventMigrationBuilder<SubscriptionStateChanged, SubscriptionStateChangedV1> builder)
    {
    }

    public override void Downcast(IEventMigrationBuilder<SubscriptionStateChangedV1, SubscriptionStateChanged> builder)
    {
    }

    public override void MapValues(IEventValueMapBuilder<SubscriptionStateChanged, SubscriptionStateChangedV1> builder) =>
        builder.For(current => current.State, previous => previous.State, map => map
            .Map(SubscriptionStateV1.Unknown, SubscriptionState.Unspecified)
            .Map(SubscriptionStateV1.Active, SubscriptionState.Running)
            .Map(SubscriptionStateV1.Cancelled, SubscriptionState.Stopped));
}
