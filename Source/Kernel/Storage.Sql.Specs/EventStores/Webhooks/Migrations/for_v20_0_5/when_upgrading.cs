// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.Migrations.for_v20_0_5;

public class when_upgrading : Specification
{
    AddColumnOperation _column;
    void Because() => _column = new v20_0_5().UpOperations.OfType<AddColumnOperation>().Single();
    [Fact] void should_sort_after_the_existing_webhook_migration() => string.CompareOrdinal(typeof(v20_0_5).GetCustomAttributes(false).OfType<MigrationAttribute>().Single().Id, typeof(v19_43_0).GetCustomAttributes(false).OfType<MigrationAttribute>().Single().Id).ShouldBeGreaterThan(0);
    [Fact] void should_add_the_signing_policy_column() => _column.Name.ShouldEqual(nameof(WebhookDefinition.Signing));
    [Fact] void should_keep_existing_webhooks_unsigned() => _column.DefaultValue.ShouldEqual(0);
    [Fact] void should_require_a_signing_policy() => _column.IsNullable.ShouldBeFalse();
    [Fact] void should_target_the_webhook_definition_table() => _column.Table.ShouldEqual(WellKnownTableNames.WebhookDefinitions);
    [Fact] void should_remove_only_the_signing_column_on_downgrade() => new v20_0_5().DownOperations.OfType<DropColumnOperation>().Single().Name.ShouldEqual(nameof(WebhookDefinition.Signing));
}
