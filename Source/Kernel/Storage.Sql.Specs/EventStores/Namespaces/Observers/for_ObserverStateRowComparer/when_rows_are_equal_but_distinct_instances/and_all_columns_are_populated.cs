// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateRowComparer.when_rows_are_equal_but_distinct_instances;

public class and_all_columns_are_populated : Specification
{
    bool _result;

    void Because() => _result = ObserverStateRowComparer.Instance.Equals(ComparerRows.Create(), ComparerRows.Create());

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
}
