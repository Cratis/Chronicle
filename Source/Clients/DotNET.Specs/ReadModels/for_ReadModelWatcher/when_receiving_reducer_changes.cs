// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_receiving_reducer_changes : given.a_watcher
{
    readonly Subject<Contracts.ReadModels.ReadModelChangeset> _frames = new();
    readonly List<ReadModelChangeset<SomeModel>> _changes = [];
    readonly Guid _correlation = Guid.NewGuid();
    readonly DateTimeOffset _occurred = DateTimeOffset.UtcNow;
    Contracts.ReadModels.WatchRequest _request;

    void Establish()
    {
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        var handler = Substitute.For<IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequenceId.Outbox);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(handler);
        _readModelsService.Watch(Arg.Do<Contracts.ReadModels.WatchRequest>(request => _request = request)).Returns(_frames);
        _watcher.Observable.Subscribe(_changes.Add);
        _watcher.Start();
    }

    void Because()
    {
        foreach (var type in new[] { Contracts.ReadModels.ReadModelChangeType.Added, Contracts.ReadModels.ReadModelChangeType.Modified, Contracts.ReadModels.ReadModelChangeType.Removed })
        {
            _frames.OnNext(new()
            {
                Namespace = _eventStoreNamespace,
                ModelKey = "key",
                ReadModel = "{}",
                Removed = type == Contracts.ReadModels.ReadModelChangeType.Removed,
                ChangeType = type,
                EventSequenceNumber = 42,
                CorrelationId = _correlation,
                Occurred = _occurred
            });
        }
    }

    void Destroy()
    {
        _watcher.Dispose();
        _frames.Dispose();
    }

    [Fact] void should_watch_the_handlers_sequence() => _request.EventSequenceId.ShouldEqual(EventSequenceId.Outbox.Value);
    [Fact] void should_preserve_change_types() => _changes.Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed]);
    [Fact] void should_deserialize_live_models() => _changes.Take(2).All(_ => _.ReadModel is not null).ShouldBeTrue();
    [Fact] void should_not_deserialize_removed_models() => _changes[^1].ReadModel.ShouldBeNull();
    [Fact] void should_preserve_event_store() => _changes.TrueForAll(_ => _.ChangeContext!.EventStore == _eventStoreName).ShouldBeTrue();
    [Fact] void should_preserve_namespace() => _changes.TrueForAll(_ => _.ChangeContext!.Namespace == _eventStoreNamespace).ShouldBeTrue();
    [Fact] void should_preserve_key() => _changes.TrueForAll(_ => _.ModelKey.Value == "key" && _.ChangeContext!.EventSourceId.Value == "key").ShouldBeTrue();
    [Fact] void should_preserve_sequence_number() => _changes.TrueForAll(_ => _.ChangeContext!.SequenceNumber == (EventSequenceNumber)42).ShouldBeTrue();
    [Fact] void should_preserve_correlation() => _changes.TrueForAll(_ => _.ChangeContext!.CorrelationId.Value == _correlation).ShouldBeTrue();
    [Fact] void should_preserve_occurred() => _changes.TrueForAll(_ => _.ChangeContext!.Occurred == _occurred).ShouldBeTrue();
}
