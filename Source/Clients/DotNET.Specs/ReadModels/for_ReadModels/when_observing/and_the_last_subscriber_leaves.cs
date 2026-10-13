// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class and_the_last_subscriber_leaves : given.a_collection_stream
{
    bool _disposedWithSubscriber;

    void Because()
    {
        _observation = _readModels.Observe<Row>();
        Subscribe();
        var second = _observation.Subscribe(_ => { });
        _subscription.Dispose();
        _disposedWithSubscriber = _disposed;
        second.Dispose();
    }

    [Fact] void should_keep_the_stream_while_a_subscriber_remains() => _disposedWithSubscriber.ShouldBeFalse();
    [Fact] void should_close_the_server_stream() => _disposed.ShouldBeTrue();
    [Fact] void should_leave_the_watcher_manager_untouched() => _readModelWatcherManager.ReceivedCalls().ShouldBeEmpty();
}
