// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;
using Contract = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_watching_a_reducer_on_another_event_sequence : given.a_watcher
{
    readonly List<Contract.WatchRequest> _requests = [];
    IReducerHandler _handler;

    void Establish()
    {
        _handler = Substitute.For<IReducerHandler>();
        _handler.EventSequenceId.Returns((EventSequenceId)"another-sequence");
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(_handler);
        _readModelsService.Watch(Arg.Do<Contract.WatchRequest>(_requests.Add)).Returns(Observable.Empty<Contract.ReadModelChangeset>());
    }

    void Because()
    {
        _watcher.Start();
        _handler.EventSequenceId.Returns((EventSequenceId)"reconnected-sequence");
        _watcher.Start();
    }

    void Destroy() => _watcher.Dispose();

    [Fact] void should_use_the_reducers_event_sequence() => _requests[0].EventSequenceId.ShouldEqual("another-sequence");
    [Fact] void should_resolve_the_sequence_again_when_restarting() => _requests[1].EventSequenceId.ShouldEqual("reconnected-sequence");
}
