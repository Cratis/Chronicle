// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_validating_cycle_receipts;

/// <summary>
/// The Migrate and Verify admissions of one sink are separate: neither satisfies the other.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_schema_modes_share_caches<THarness> : given.a_receipt_cycle<THarness>
    where THarness : ISqlSinkHarness
{
    Exception? _verifyAfterMigrate;
    bool _tableAfterVerify;
    Exception? _migrateAfterVerify;
    bool _tableAfterMigrate;

    async Task Establish()
    {
        await Seed();
        await Provision(_sink, "provisioning", 2);
    }

    async Task Because()
    {
        // The same sink instance has Migrate's admission cached; Verify must still check for itself.
        await DropReceiptTable();
        _verify = true;
        _capture.Clear();
        _verifyAfterMigrate = await Attempt(_sink, "verify", 3);
        _tableAfterVerify = await ReceiptTableExists();

        // A sink with Verify's admission cached must leave Migrate to ensure the table.
        _verify = false;
        var sink = AnotherSink();
        await Provision(sink, "restored", 4);
        _verify = true;
        await MultiWrite(sink, "admitted", 5);
        await DropReceiptTable();
        _verify = false;
        _migrateAfterVerify = await Attempt(sink, "migrate", 6);
        _tableAfterMigrate = await ReceiptTableExists();
    }

    [Fact] public void should_not_let_migrate_satisfy_verify() => _verifyAfterMigrate.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] public void should_not_create_the_table_for_verify() => _tableAfterVerify.ShouldBeFalse();
    [Fact] public void should_not_let_verify_satisfy_migrate() => _migrateAfterVerify.ShouldBeNull();
    [Fact] public void should_let_migrate_provision_the_table() => _tableAfterMigrate.ShouldBeTrue();
}
