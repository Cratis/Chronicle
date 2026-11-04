// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts;

public class and_a_quarantined_event_store_has_no_receipts_on_sqlite : given.provisioned_namespaces
{
    void Establish()
    {
        Execute(_receiptDatabase, $"DROP TABLE {Table}");
        _resourceNames.Publish(new ResourceNameAuditResult(["shipping"], [], []));
    }

    async Task Because() => _error = await Catch.Exception(() => Start());

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_report_the_event_store_as_skipped() => _verification.SkippedResources.EventStores.ShouldContainOnly((EventStoreName)"shipping");
}
