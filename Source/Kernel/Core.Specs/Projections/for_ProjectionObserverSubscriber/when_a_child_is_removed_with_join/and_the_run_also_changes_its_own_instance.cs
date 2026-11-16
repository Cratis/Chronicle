// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber.when_a_child_is_removed_with_join;

public class and_the_run_also_changes_its_own_instance : given.a_run_with_child_removals
{
    protected override IEnumerable<Change> ChangesOfTheRun =>
    [
        new PropertiesChanged<ExpandoObject>(new ExpandoObject(), []),
        new ChildRemovedFromAll(new PropertyPath("groups"), new PropertyPath("groupId"), Guid.NewGuid(), ArrayIndexers.NoIndexers)
    ];

    async Task Because() => await _subscriber.OnNext("the-removed-group", [_event], new(null));

    [Fact] void should_notify_the_partition() => _notifier.Received(1).Notify(Arg.Any<EventStoreNamespaceName>(), "the-removed-group", Arg.Any<JsonObject>(), Arg.Any<ReadModelChangeContext>());
}
