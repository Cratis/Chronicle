// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_client_reconnects : given.a_watcher
{
    IObservable<ReadModelChangeset> _observable;
    IDisposable _firstSubscription;
    IDisposable _secondSubscription;

    void Establish()
    {
        _observable = Substitute.For<IObservable<ReadModelChangeset>>();
        _firstSubscription = Substitute.For<IDisposable>();
        _secondSubscription = Substitute.For<IDisposable>();
        _observable.Subscribe(Arg.Any<IObserver<ReadModelChangeset>>()).Returns(_firstSubscription, _secondSubscription);
        _readModelsService.Watch(Arg.Any<WatchRequest>()).Returns(_observable);
        _watcher.Start();
    }

    void Because()
    {
        _connection.Lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _connection.Lifecycle.OnConnected += Raise.Event<Connected>();
    }

    [Fact] void should_re_establish_the_watch_stream() => _readModelsService.Received(2).Watch(Arg.Any<WatchRequest>());
    [Fact] void should_dispose_the_previous_subscription() => _firstSubscription.Received(1).Dispose();
}
