// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerObservers.when_notifying_change;

public class with_removed_model : given.a_reducer_observers
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    ReadModelKey _modelKey;
    EventStoreNamespaceName _namespace;
    ReducerChangeset<MyReadModel> _receivedChangeset;

    void Establish()
    {
        _modelKey = "test-key";
        _namespace = "test-namespace";

        var watcher = _observers.GetWatcher<MyReadModel>();
        watcher.Observable.Subscribe(changeset => _receivedChangeset = changeset);
    }

    void Because() => _observers.NotifyChange<MyReadModel>(_namespace, _modelKey, null, true);

    [Fact] void should_emit_changeset() => _receivedChangeset.ShouldNotBeNull();
    [Fact] void should_have_correct_namespace() => _receivedChangeset.Namespace.ShouldEqual(_namespace);
    [Fact] void should_have_correct_model_key() => _receivedChangeset.ModelKey.ShouldEqual(_modelKey);
    [Fact] void should_have_null_read_model() => _receivedChangeset.ReadModel.ShouldBeNull();
    [Fact] void should_be_removed() => _receivedChangeset.Removed.ShouldBeTrue();
}
