// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Reducers;
using Contract = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_receiving_a_removed_reducer_model : given.a_watcher
{
    readonly Subject<Contract.ReadModelChangeset> _server = new();
    readonly List<ReadModelChangeset<Model>> _changes = [];
    ReadModelWatcher<Model> _reducerWatcher;
    IDisposable _subscription;

    void Establish()
    {
        _eventStore.Reducers.HasFor<Model>().Returns(true);
        _eventStore.Reducers.GetHandlerForReadModelType(typeof(Model)).Returns(Substitute.For<IReducerHandler>());
        _readModelsService.Watch(Arg.Any<Contract.WatchRequest>()).Returns(_server);
        _reducerWatcher = new(_eventStore, () => { }, new JsonSerializerOptions { RespectRequiredConstructorParameters = true });
        _subscription = _reducerWatcher.Observable.Subscribe(_changes.Add);
        _reducerWatcher.Start();
    }

    void Because()
    {
        foreach (var json in new[] { "{\"Value\":\"prior state\"}", "{}" })
        {
            _server.OnNext(new()
            {
                Namespace = _eventStoreNamespace,
                ModelKey = "key",
                ReadModel = json,
                Removed = true,
                ChangeType = Contract.ReadModelChangeType.Removed
            });
        }
    }

    void Destroy()
    {
        _subscription.Dispose();
        _reducerWatcher.Dispose();
        _watcher.Dispose();
        _server.Dispose();
    }

    [Fact] void should_receive_both_removals_without_deserializing() => _changes.Count.ShouldEqual(2);
    [Fact] void should_not_expose_the_prior_model() => _changes.TrueForAll(_ => _.ReadModel is null).ShouldBeTrue();
    [Fact] void should_mark_the_frames_as_removed() => _changes.TrueForAll(_ => _.Removed && _.ChangeType == ReadModelChangeType.Removed).ShouldBeTrue();

    public record Model(string Value);
}
