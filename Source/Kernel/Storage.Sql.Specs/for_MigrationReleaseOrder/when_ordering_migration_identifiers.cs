// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_MigrationReleaseOrder;

public class when_ordering_migration_identifiers : Specification
{
    string[] _ordered;

    void Because() => _ordered =
    [
        .. new[]
        {
            "NS-Observers-v19_104_0_0",
            "NS-Alerts-v19_32_0",
            "without-a-release",
            "NS-Observers-v19_83_1_1",
            "NS-Observers-v15_0_0",
            "NS-Observers-v19_15_0",
            "NS-Observers-v19_83_1_0",
            "NS-InFlightEvents-v19_93_0",
            "NS-ObserverHandledCounts-v19_93_0",
            "NS-Observers-v19_83_1",
        }.Order(MigrationReleaseOrder.Instance)
    ];

    [Fact] void should_order_by_release_then_by_identifier() => _ordered.ShouldEqual(
    [
        "without-a-release",
        "NS-Observers-v15_0_0",
        "NS-Observers-v19_15_0",
        "NS-Alerts-v19_32_0",
        "NS-Observers-v19_83_1",
        "NS-Observers-v19_83_1_0",
        "NS-Observers-v19_83_1_1",
        "NS-InFlightEvents-v19_93_0",
        "NS-ObserverHandledCounts-v19_93_0",
        "NS-Observers-v19_104_0_0",
    ]);
}
