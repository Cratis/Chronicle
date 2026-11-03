// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Contract = Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelWatcher;

public class when_receiving_a_removed_projection_model : given.a_watcher
{
    readonly Subject<Contract.ReadModelChangeset> _server = new();
    ReadModelWatcher<Model> _projectionWatcher;
    ReadModelChangeset<Model> _change;
    IDisposable _subscription;

    void Establish()
    {
        _eventStore.Reducers.HasFor<Model>().Returns(false);
        _readModelsService.Watch(Arg.Any<Contract.WatchRequest>()).Returns(_server);
        _projectionWatcher = new(_eventStore, () => { }, JsonSerializerOptions.Default);
        _subscription = _projectionWatcher.Observable.Subscribe(change => _change = change);
        _projectionWatcher.Start();
    }

    void Because() => _server.OnNext(new()
    {
        Namespace = _eventStoreNamespace,
        ModelKey = "key",
        ReadModel = "{\"Value\":\"prior state\"}",
        Removed = true,
        ChangeType = Contract.ReadModelChangeType.Removed
    });

    void Destroy()
    {
        _subscription.Dispose();
        _projectionWatcher.Dispose();
        _watcher.Dispose();
        _server.Dispose();
    }

    [Fact] void should_keep_deserializing_the_projection_payload() => _change.ReadModel!.Value.ShouldEqual("prior state");
    [Fact] void should_preserve_the_removed_label() => _change.ChangeType.ShouldEqual(ReadModelChangeType.Removed);

    public record Model(string Value);
}
