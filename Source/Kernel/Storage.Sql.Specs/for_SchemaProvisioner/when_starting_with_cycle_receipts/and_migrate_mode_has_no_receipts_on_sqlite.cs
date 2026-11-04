// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts;

public class and_migrate_mode_has_no_receipts_on_sqlite : given.provisioned_namespaces
{
    void Establish() => Execute(_receiptDatabase, $"DROP TABLE {Table}");

    async Task Because() => _error = await Catch.Exception(() => Start(SchemaMode.Migrate));

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_leave_receipt_creation_until_access() => Scalar(_receiptDatabase, $"SELECT COUNT(*) FROM sqlite_master WHERE name = '{Table}'").ShouldEqual(0);
}
