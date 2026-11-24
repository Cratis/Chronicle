// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class an_active_reducer : given.reducer_watch_routes
{
    bool _returnedBeforeAcknowledgment;
    IReadModelWatcher<Model> _readyWatcher;

    async Task Because()
    {
        var acknowledged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _watcher.Subscribed.Returns(acknowledged.Task);
        _watcher.Observable.Returns(_ =>
        {
            _returnedBeforeAcknowledgment = !acknowledged.Task.IsCompleted;
            return _server;
        });
        await Task.Run(Subscribe).WaitAsync(TimeSpan.FromSeconds(10));
        acknowledged.SetResult();
        _readyWatcher = _readModels.GetWatcherFor<Model>();
        EmitBoth();
    }

    [Fact] void should_return_before_the_kernel_acknowledgment() => _returnedBeforeAcknowledgment.ShouldBeTrue();
    [Fact] void should_expose_the_reducer_readiness_signal() => _readyWatcher.ShouldEqual(_watcher);
    [Fact] void should_only_forward_server_changes() => _changes.Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified]);
    [Fact] void should_preserve_the_server_context() => _changes.Select(_ => _.ChangeContext).ShouldEqual([_context, _context]);
    [Fact] void should_not_subscribe_to_local_changes() => _reducerObservers.DidNotReceive().GetWatcher<Model>();
}
