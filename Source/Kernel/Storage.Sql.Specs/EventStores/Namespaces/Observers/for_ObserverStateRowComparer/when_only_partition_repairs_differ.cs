// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateRowComparer;

public class when_only_partition_repairs_differ : Specification
{
    bool _result;

    void Because()
    {
        var original = ComparerRows.Create();
        var changed = ComparerRows.Create();
        changed.PartitionRepairs = "[]";
        _result = ObserverStateRowComparer.Instance.Equals(original, changed);
    }

    [Fact] void should_not_treat_the_repair_write_as_unchanged() => _result.ShouldBeFalse();
}
