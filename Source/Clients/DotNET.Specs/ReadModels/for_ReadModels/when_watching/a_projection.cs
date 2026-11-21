// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class a_projection : given.reducer_watch_routes
{
    void Establish()
    {
        _reducers.HasFor<Model>().Returns(false);
        _projections.HasFor<Model>().Returns(true);
    }

    void Because()
    {
        Subscribe();
        EmitBoth();
    }

    [Fact] void should_keep_forwarding_server_changes() => _changes.Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified]);
    [Fact] void should_not_subscribe_to_local_reducer_changes() => _reducerObservers.DidNotReceive().GetWatcher<Model>();
}
