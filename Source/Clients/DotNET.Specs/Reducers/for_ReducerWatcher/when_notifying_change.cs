// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerWatcher;

public class when_notifying_change : Specification
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    ReducerWatcher<MyReadModel> _watcher;
    MyReadModel _model;
    ReadModelKey _modelKey;
    EventStoreNamespaceName _namespace;
    ReducerChangeset<MyReadModel> _receivedChangeset;

    void Establish()
    {
        _watcher = new ReducerWatcher<MyReadModel>();
        _model = new MyReadModel { Name = "Test" };
        _modelKey = "test-key";
        _namespace = "test-namespace";

        _watcher.Observable.Subscribe(changeset => _receivedChangeset = changeset);
    }

    void Because() => _watcher.NotifyChange(_namespace, _modelKey, _model, false);

    [Fact] void should_emit_changeset_to_observable() => _receivedChangeset.ShouldNotBeNull();
    [Fact] void should_have_correct_namespace() => _receivedChangeset.Namespace.ShouldEqual(_namespace);
    [Fact] void should_have_correct_model_key() => _receivedChangeset.ModelKey.ShouldEqual(_modelKey);
    [Fact] void should_have_correct_read_model() => _receivedChangeset.ReadModel.ShouldEqual(_model);
    [Fact] void should_not_be_removed() => _receivedChangeset.Removed.ShouldBeFalse();
}
