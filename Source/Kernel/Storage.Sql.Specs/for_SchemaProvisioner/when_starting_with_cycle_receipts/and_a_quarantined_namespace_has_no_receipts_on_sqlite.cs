// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts;

public class and_a_quarantined_namespace_has_no_receipts_on_sqlite : given.provisioned_namespaces
{
    void Establish()
    {
        Execute(_receiptDatabase, $"DROP TABLE {Table}");
        _resourceNames.Publish(new ResourceNameAuditResult([], [new ResourceOwner("shipping", "staging")], []));
    }

    async Task Because() => _error = await Catch.Exception(() => Start());

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_report_the_namespace_as_skipped() => _verification.SkippedResources.Namespaces.ShouldContainOnly(((EventStoreName)"shipping", (EventStoreNamespaceName)"staging"));
    [Fact] void should_not_create_the_table() => Scalar(_receiptDatabase, $"SELECT COUNT(*) FROM sqlite_master WHERE name = '{Table}'").ShouldEqual(0);
}
