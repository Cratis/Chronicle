// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_PartitionReplayCompletionMigration;

public class when_ordering_after_the_released_handled_counts : Specification
{
    string _released;
    string _completions;
    int _comparison;

    void Establish()
    {
        _released = typeof(Migrations.v19_110_0).GetCustomAttribute<MigrationAttribute>()!.Id;
        _completions = typeof(Migrations.v19_111_0_0).GetCustomAttribute<MigrationAttribute>()!.Id;
    }

    void Because() => _comparison = MigrationReleaseOrder.Instance.Compare(_completions, _released);

    [Fact] void should_order_after_the_released_migration() => _comparison.ShouldBeGreaterThan(0);
    [Fact] void should_belong_to_a_later_release() => _completions.ShouldEqual("NS-Observers-v19_111_0_0");
}
