// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using ContractReadModels = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class again_after_a_reducer_stream_failed : given.reducer_watch_routes
{
    readonly Subject<ContractReadModels.ReadModelChangeset> _replacement = new();
    readonly InvalidOperationException _error = new("Stream failed before acknowledgment");
    ContractReadModels.IReadModels _service;
    Task _failed;
    Task _retried;
    bool _readyBeforeAcknowledgment;

    void Establish()
    {
        _eventStore.Reducers.Returns(_reducers);
        _handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        _service = Substitute.For<ContractReadModels.IReadModels>();
        _services.ReadModels.Returns(_service);
        _service.Watch(Arg.Any<ContractReadModels.WatchRequest>()).Returns(
            Observable.Throw<ContractReadModels.ReadModelChangeset>(_error), _replacement);
        _watcher = new ReadModelWatcher<Model>(_eventStore, () => { }, _jsonSerializerOptions);
        _readModelWatcherManager.GetWatcher<Model>().Returns(_watcher);
    }

    async Task Because()
    {
        await Task.Run(() => _readModels.Watch<Model>()).WaitAsync(TimeSpan.FromSeconds(10));
        _failed = _watcher.Subscribed;
        _subscription = await Task.Run(() => _readModels.Watch<Model>().Subscribe(_changes.Add)).WaitAsync(TimeSpan.FromSeconds(10));
        _retried = _watcher.Subscribed;
        _readyBeforeAcknowledgment = _retried.IsCompleted;
        _replacement.OnNext(new() { Subscribed = true });
        _replacement.OnNext(new() { ReadModel = "{}", ChangeType = ContractReadModels.ReadModelChangeType.Added });
    }

    [Fact] void should_surface_the_immediate_failure() => _failed.IsFaulted.ShouldBeTrue();
    [Fact] void should_preserve_the_failure() => _failed.Exception.InnerException.ShouldEqual(_error);
    [Fact] void should_restart_the_cached_watcher() => _service.Received(2).Watch(Arg.Any<ContractReadModels.WatchRequest>());
    [Fact] void should_wait_for_the_replacement_acknowledgment() => _readyBeforeAcknowledgment.ShouldBeFalse();
    [Fact] void should_confirm_the_replacement_subscription() => _retried.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_receive_the_replacement_stream() => _changes.Count.ShouldEqual(1);

    void Destroy()
    {
        _watcher.Stop();
        _replacement.Dispose();
    }
}
