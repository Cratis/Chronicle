// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_reducer_emits_removed_changeset : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    Subject<ReducerChangeset<MyReadModel>> _reducerObservable;
    ReadModelChangeset<MyReadModel> _receivedChangeset;
    ReadModelKey _modelKey;
    EventStoreNamespaceName _namespace;

    void Establish()
    {
        _modelKey = "test-key";
        _namespace = "test-namespace";

        _reducerObservable = new Subject<ReducerChangeset<MyReadModel>>();
        _reducers.HasFor<MyReadModel>().Returns(true);
        _reducers.Watch<MyReadModel>().Returns(_reducerObservable);

        var observable = _readModels.Watch<MyReadModel>();
        observable.Subscribe(changeset => _receivedChangeset = changeset);
    }

    void Because() => _reducerObservable.OnNext(new ReducerChangeset<MyReadModel>(_namespace, _modelKey, null, true));

    [Fact] void should_map_namespace() => _receivedChangeset.Namespace.ShouldEqual(_namespace);
    [Fact] void should_map_model_key() => _receivedChangeset.ModelKey.ShouldEqual(_modelKey);
    [Fact] void should_have_null_read_model() => _receivedChangeset.ReadModel.ShouldBeNull();
    [Fact] void should_map_removed_flag() => _receivedChangeset.Removed.ShouldBeTrue();
}
