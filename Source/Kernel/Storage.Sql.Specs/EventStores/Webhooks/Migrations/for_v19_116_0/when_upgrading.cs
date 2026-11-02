// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.Migrations.for_v19_116_0;

public class when_upgrading : Specification
{
    AddColumnOperation _column;
    void Because() => _column = new v19_116_0().UpOperations.OfType<AddColumnOperation>().Single();
    [Fact] void should_sort_after_webhook_definition_creation() => MigrationReleaseOrder.Instance.Compare(MigrationId<v19_116_0>(), MigrationId<v15_0_0>()).ShouldBeGreaterThan(0);
    [Fact] void should_sort_after_the_generation_delivery_policy() => MigrationReleaseOrder.Instance.Compare(MigrationId<v19_116_0>(), MigrationId<v19_43_0>()).ShouldBeGreaterThan(0);
    [Fact] void should_add_the_delivery_format_column() => _column.Name.ShouldEqual(nameof(WebhookDefinition.DeliveryFormat));
    [Fact] void should_keep_existing_webhooks_in_legacy_format() => _column.DefaultValue.ShouldEqual(0);
    [Fact] void should_target_the_webhook_definition_table() => _column.Table.ShouldEqual(WellKnownTableNames.WebhookDefinitions);

    static string MigrationId<T>() => typeof(T).GetCustomAttributes(false).OfType<MigrationAttribute>().Single().Id;
}
