// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_the_reducer_is_observed_by_the_kernel : given.a_reducer_backed_model
{
    ReadModelChangeset<Model> _frame;

    void Because()
    {
        Watch();
        _frame = new("test-namespace", "key", _model, false, ReadModelChangeType.Added, _context);
        _server.OnNext(_frame);
    }

    [Fact] void should_use_the_kernel_watcher() => _readModelWatcherManager.Received(1).GetWatcher<Model>();
    [Fact] void should_not_use_the_local_watcher() => _reducerObservers.DidNotReceive().GetWatcher<Model>();
    [Fact] void should_pass_the_frame_unchanged() => _changes.Single().ShouldEqual(_frame);
    [Fact] void should_preserve_the_context() => _changes.Single().ChangeContext.ShouldEqual(_context);
}
