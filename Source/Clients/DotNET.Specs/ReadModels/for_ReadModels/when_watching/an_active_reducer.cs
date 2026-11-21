// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class an_active_reducer : given.reducer_watch_routes
{
    void Because()
    {
        Subscribe();
        EmitBoth();
    }

    [Fact] void should_only_forward_server_changes() => _changes.Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified]);
    [Fact] void should_preserve_the_server_context() => _changes.Select(_ => _.ChangeContext).ShouldEqual([_context, _context]);
    [Fact] void should_not_subscribe_to_local_changes() => _reducerObservers.DidNotReceive().GetWatcher<Model>();
}
