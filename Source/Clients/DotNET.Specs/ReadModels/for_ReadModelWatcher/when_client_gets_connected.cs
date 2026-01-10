// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_client_gets_connected : given.a_watcher
{
    IObservable<ReadModelChangeset> _observable;

    void Establish()
    {
        _observable = Substitute.For<IObservable<ReadModelChangeset>>();
        _readModelsService.Watch(Arg.Any<WatchRequest>()).Returns(_observable);
    }

    void Because() => _connection.Lifecycle.OnConnected += Raise.Event<Connected>();

    [Fact] void should_start_watching() => _readModelsService.Received().Watch(Arg.Any<WatchRequest>());
}
