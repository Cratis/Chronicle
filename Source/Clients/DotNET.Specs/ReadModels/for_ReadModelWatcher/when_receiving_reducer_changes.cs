// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Reducers;
using Contract = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_receiving_reducer_changes : given.a_watcher
{
    readonly Subject<Contract.ReadModelChangeset> _server = new();
    readonly List<ReadModelChangeset<SomeModel>> _changes = [];
    readonly Guid _correlationId = Guid.NewGuid();
    readonly DateTimeOffset _occurred = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
    IDisposable _subscription;

    void Establish()
    {
        _eventStore.Reducers.HasFor<SomeModel>().Returns(true);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(SomeModel)).Returns(Substitute.For<IReducerHandler>());
        _readModelsService.Watch(Arg.Any<Contract.WatchRequest>()).Returns(_server);
        _subscription = _watcher.Observable.Subscribe(_changes.Add);
        _watcher.Start();
    }

    void Because()
    {
        foreach (var changeType in new[] { Contract.ReadModelChangeType.Added, Contract.ReadModelChangeType.Modified, Contract.ReadModelChangeType.Removed })
        {
            _server.OnNext(new()
            {
                Namespace = _eventStoreNamespace,
                ModelKey = "key",
                ReadModel = "{}",
                ChangeType = changeType,
                Removed = changeType == Contract.ReadModelChangeType.Removed,
                EventSequenceNumber = 42,
                CorrelationId = _correlationId,
                Occurred = _occurred
            });
        }
    }

    void Destroy()
    {
        _subscription.Dispose();
        _watcher.Dispose();
        _server.Dispose();
    }

    [Fact] void should_map_the_server_change_types() => _changes.Select(_ => _.ChangeType).ShouldEqual([ReadModelChangeType.Added, ReadModelChangeType.Modified, ReadModelChangeType.Removed]);
    [Fact] void should_carry_the_sequence_number() => _changes.TrueForAll(_ => _.ChangeContext!.SequenceNumber.Value == 42).ShouldBeTrue();
    [Fact] void should_carry_the_occurrence_time() => _changes.TrueForAll(_ => _.ChangeContext!.Occurred == _occurred).ShouldBeTrue();
    [Fact] void should_carry_the_correlation_id() => _changes.TrueForAll(_ => _.ChangeContext!.CorrelationId.Value == _correlationId).ShouldBeTrue();
    [Fact] void should_carry_the_model_key() => _changes.TrueForAll(_ => _.ChangeContext!.EventSourceId.Value == "key").ShouldBeTrue();
}
