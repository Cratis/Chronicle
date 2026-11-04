// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts;

public class and_another_namespace_has_no_receipts_while_one_is_quarantined_on_sqlite : given.provisioned_namespaces
{
    void Establish()
    {
        Execute(_receiptDatabase, $"DROP TABLE {Table}");
        _resourceNames.Publish(new ResourceNameAuditResult([], [new ResourceOwner("orders", "production")], []));
    }

    async Task Because() => _error = await Catch.Exception(() => Start());

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<SchemaNotCurrent>();
}
