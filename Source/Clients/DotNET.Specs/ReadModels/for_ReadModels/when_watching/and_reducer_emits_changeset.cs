// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_reducer_emits_changeset : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    Subject<ReducerChangeset<MyReadModel>> _reducerObservable;
    ReadModelChangeset<MyReadModel> _receivedChangeset;
    MyReadModel _model;
    ReadModelKey _modelKey;
    EventStoreNamespaceName _namespace;

    void Establish()
    {
        _model = new MyReadModel { Name = "Test" };
        _modelKey = "test-key";
        _namespace = "test-namespace";

        _reducerObservable = new Subject<ReducerChangeset<MyReadModel>>();
        _reducers.HasFor<MyReadModel>().Returns(true);
        _reducers.Watch<MyReadModel>().Returns(_reducerObservable);

        var observable = _readModels.Watch<MyReadModel>();
        observable.Subscribe(changeset => _receivedChangeset = changeset);
    }

    void Because() => _reducerObservable.OnNext(new ReducerChangeset<MyReadModel>(_namespace, _modelKey, _model, false));

    [Fact] void should_map_namespace() => _receivedChangeset.Namespace.ShouldEqual(_namespace);
    [Fact] void should_map_model_key() => _receivedChangeset.ModelKey.ShouldEqual(_modelKey);
    [Fact] void should_map_read_model() => _receivedChangeset.ReadModel.ShouldEqual(_model);
    [Fact] void should_map_removed_flag() => _receivedChangeset.Removed.ShouldBeFalse();
}
