// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_a_reducer_removes_a_required_model : given.a_watcher
{
    readonly Subject<Contracts.ReadModels.ReadModelChangeset> _frames = new();
    ReadModelWatcher<RequiredModel> _requiredWatcher;
    ReadModelChangeset<RequiredModel> _change;
    Exception _error;

    void Establish()
    {
        _eventStore.Reducers.HasFor<RequiredModel>().Returns(true);
        var handler = Substitute.For<IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(RequiredModel)).Returns(handler);
        _readModelsService.Watch(Arg.Any<Contracts.ReadModels.WatchRequest>()).Returns(_frames);
        _requiredWatcher = new(_eventStore, () => { }, JsonSerializerOptions.Default);
        _requiredWatcher.Observable.Subscribe(change => _change = change);
        _requiredWatcher.Start();
    }

    void Because() => _error = Catch.Exception(() => _frames.OnNext(new()
    {
        Namespace = _eventStoreNamespace,
        ModelKey = "removed",
        ReadModel = "{}",
        Removed = true,
        ChangeType = Contracts.ReadModels.ReadModelChangeType.Removed,
        EventSequenceNumber = 42,
        CorrelationId = Guid.NewGuid(),
        Occurred = DateTimeOffset.UtcNow
    }));

    void Destroy()
    {
        _requiredWatcher.Dispose();
        _watcher.Dispose();
        _frames.Dispose();
    }

    [Fact] void should_not_deserialize_the_removal_payload() => _error.ShouldBeNull();
    [Fact] void should_emit_a_default_model() => _change.ReadModel.ShouldBeNull();
    [Fact] void should_report_removed() => _change.ChangeType.ShouldEqual(ReadModelChangeType.Removed);
    [Fact] void should_preserve_the_sequence_number() => _change.ChangeContext!.SequenceNumber.ShouldEqual((EventSequenceNumber)42);

    public class RequiredModel
    {
        public required string Name { get; init; }
    }
}
