// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateRowComparer.when_running_state_changed_at_differs;

public class and_only_the_timestamp_is_different : Specification
{
    bool _result;

    void Because()
    {
        var changed = ComparerRows.Create();
        changed.RunningStateChangedAt = changed.RunningStateChangedAt!.Value.AddSeconds(1);
        _result = ObserverStateRowComparer.Instance.Equals(ComparerRows.Create(), changed);
    }

    [Fact] void should_not_be_equal() => _result.ShouldBeFalse();
}
