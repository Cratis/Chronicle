// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_an_instance_beyond_the_default_window_changes : given.materialized_reactors
{
    void Because()
    {
        _items[50] = _items[50] with { Name = "modified" };
        _instances.OnNext(_items);
    }

    [Fact] void should_observe_all_instances() => _take.ShouldEqual(InstanceCount.Unlimited);
    [Fact] void should_dispatch_only_the_modified_instance() => _changes.ShouldEqual([(_items[50], ReadModelChangeType.Modified)]);
}
