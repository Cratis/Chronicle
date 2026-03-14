// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_client_gets_disconnected : given.a_watcher
{
    IObservable<ReadModelChangeset> _observable;
    IDisposable _subscription;

    void Establish()
    {
        _observable = Substitute.For<IObservable<ReadModelChangeset>>();
        _subscription = Substitute.For<IDisposable>();
        _observable.Subscribe(Arg.Any<IObserver<ReadModelChangeset>>()).Returns(_subscription);
        _readModelsService.Watch(Arg.Any<WatchRequest>()).Returns(_observable);

        // Start the watcher so _started is true and a subscription exists
        _watcher.Start();
    }

    void Because() => _connection.Lifecycle.OnDisconnected += Raise.Event<Disconnected>();

    [Fact] void should_dispose_existing_subscription() => _subscription.Received().Dispose();

    [Fact] void should_allow_restarting_on_next_connect()
    {
        // After disconnect _started is false, so calling Start() again should re-subscribe
        _readModelsService.ClearReceivedCalls();
        _watcher.Start();
        _readModelsService.Received().Watch(Arg.Any<WatchRequest>());
    }
}
