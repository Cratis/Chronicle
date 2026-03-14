// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_client_reconnects : given.a_watcher
{
    IObservable<ReadModelChangeset> _firstObservable;
    IObservable<ReadModelChangeset> _secondObservable;
    IDisposable _firstSubscription;
    IDisposable _secondSubscription;

    void Establish()
    {
        _firstObservable = Substitute.For<IObservable<ReadModelChangeset>>();
        _secondObservable = Substitute.For<IObservable<ReadModelChangeset>>();
        _firstSubscription = Substitute.For<IDisposable>();
        _secondSubscription = Substitute.For<IDisposable>();

        _firstObservable.Subscribe(Arg.Any<IObserver<ReadModelChangeset>>()).Returns(_firstSubscription);
        _secondObservable.Subscribe(Arg.Any<IObserver<ReadModelChangeset>>()).Returns(_secondSubscription);

        _readModelsService.Watch(Arg.Any<WatchRequest>())
            .Returns(_firstObservable, _secondObservable);

        // Start → Disconnect → Reconnect
        _watcher.Start();
        _connection.Lifecycle.OnDisconnected += Raise.Event<Disconnected>();
    }

    void Because() => _connection.Lifecycle.OnConnected += Raise.Event<Connected>();

    [Fact] void should_have_disposed_first_subscription() => _firstSubscription.Received().Dispose();

    [Fact] void should_create_new_watch() => _readModelsService.Received(2).Watch(Arg.Any<WatchRequest>());

    [Fact] void should_not_dispose_second_subscription() => _secondSubscription.DidNotReceive().Dispose();
}
