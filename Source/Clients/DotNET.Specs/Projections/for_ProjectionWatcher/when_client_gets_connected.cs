// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionWatcher;

public class when_client_gets_connected : given.a_watcher
{
    IObservable<ProjectionChangeset> _observable;

    void Establish()
    {
        _observable = Substitute.For<IObservable<ProjectionChangeset>>();
        _projectionsService.Watch(Arg.Any<ProjectionWatchRequest>()).Returns(_observable);
    }

    void Because() => _connection.Lifecycle.OnConnected += Raise.Event<Connected>();

    [Fact] void should_start_watching() => _projectionsService.Received().Watch(Arg.Any<ProjectionWatchRequest>());
}
