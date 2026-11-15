// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors;

public class when_an_instance_is_added : given.materialized_reactors_with_a_delivery_hook
{
    async Task Because()
    {
        _instances.OnNext([new Item("1", "original"), new Item("2", "added")]);
        await _invoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _left.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_enter_the_delivery_scope_for_the_namespace_before_activating_the_reactor() => _log.Take(2).ShouldEqual(["enter:tenant-x", "activated:tenant-x"]);
    [Fact] void should_leave_the_delivery_scope_last() => _log[^1].ShouldEqual("leave");
}
