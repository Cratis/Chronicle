// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactors.when_reconnecting;

public class and_an_instance_is_added_after_reconnect : given.materialized_reactors_on_a_lifecycle
{
    void Because()
    {
        Reconnect();
        _streams[1].OnNext([.. _items, new Item("third", "new")]);
    }

    [Fact] void should_dispatch_exactly_one_change() => _changes.Count.ShouldEqual(1);
    [Fact] void should_dispatch_the_new_instance_as_added() => _changes.Single().ShouldEqual((new Item("third", "new"), ReadModelChangeType.Added));
}
