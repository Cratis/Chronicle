// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class an_inactive_reducer : given.reducer_watch_routes
{
    protected override bool Active => false;

    void Because()
    {
        Subscribe();
        EmitBoth();
    }

    [Fact] void should_only_forward_local_changes_without_guessing_added() => _changes.Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Modified, ReadModelChangeType.Removed]);
    [Fact] void should_not_subscribe_to_the_server() => _readModelWatcherManager.DidNotReceive().GetWatcher<Model>();
}
