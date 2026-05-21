// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_receiving_subscribed_signal : given.a_watcher
{
    Subject<ReadModelChangeset> _serverObservable;
    bool _wasCompletedBeforeSignal;

    void Establish()
    {
        _serverObservable = new Subject<ReadModelChangeset>();
        _readModelsService.Watch(Arg.Any<WatchRequest>()).Returns(_serverObservable);
        _watcher.Start();
        _wasCompletedBeforeSignal = _watcher.Subscribed.IsCompleted;
    }

    void Because() => _serverObservable.OnNext(new ReadModelChangeset { Subscribed = true });

    [Fact] void should_not_be_subscribed_before_signal() => _wasCompletedBeforeSignal.ShouldBeFalse();
    [Fact] void should_be_subscribed_after_signal() => _watcher.Subscribed.IsCompletedSuccessfully.ShouldBeTrue();
}
