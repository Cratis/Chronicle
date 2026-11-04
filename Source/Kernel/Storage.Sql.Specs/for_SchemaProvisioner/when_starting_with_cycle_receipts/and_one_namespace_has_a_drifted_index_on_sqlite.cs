// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts;

public class and_one_namespace_has_a_drifted_index_on_sqlite : given.provisioned_namespaces
{
    void Establish() => Execute(_receiptDatabase, $"DROP INDEX IX_{Table}_CreatedAt");

    async Task Because() => _error = await Catch.Exception(() => Start());

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_not_repair_the_index() => Scalar(_receiptDatabase, $"SELECT COUNT(*) FROM sqlite_master WHERE name = 'IX_{Table}_CreatedAt'").ShouldEqual(0);
}
