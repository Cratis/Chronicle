// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Migrations;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

public class StockSetToV2Migrator : EventTypeMigration<StockSetV2, StockSet>
{
    public override void Upcast(IEventMigrationBuilder<StockSetV2, StockSet> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.Quantity, source => source.Quantity));

    public override void Downcast(IEventMigrationBuilder<StockSet, StockSetV2> builder) =>
        builder.Properties(properties => properties.RenamedFrom(target => target.Quantity, source => source.Quantity));
}
