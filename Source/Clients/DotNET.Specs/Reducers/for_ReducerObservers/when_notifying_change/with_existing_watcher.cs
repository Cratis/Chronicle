// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerObservers.when_notifying_change;

public class with_existing_watcher : given.a_reducer_observers
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    MyReadModel _model;
    ReadModelKey _modelKey;
    EventStoreNamespaceName _namespace;
    ReducerChangeset<MyReadModel> _receivedChangeset;

    void Establish()
    {
        _model = new MyReadModel { Name = "Test" };
        _modelKey = "test-key";
        _namespace = "test-namespace";

        var watcher = _observers.GetWatcher<MyReadModel>();
        watcher.Observable.Subscribe(changeset => _receivedChangeset = changeset);
    }

    void Because() => _observers.NotifyChange(_namespace, _modelKey, _model, false);

    [Fact] void should_emit_changeset() => _receivedChangeset.ShouldNotBeNull();
    [Fact] void should_have_correct_namespace() => _receivedChangeset.Namespace.ShouldEqual(_namespace);
    [Fact] void should_have_correct_model_key() => _receivedChangeset.ModelKey.ShouldEqual(_modelKey);
    [Fact] void should_have_correct_read_model() => _receivedChangeset.ReadModel.ShouldEqual(_model);
    [Fact] void should_not_be_removed() => _receivedChangeset.Removed.ShouldBeFalse();
}
