// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerObservers.when_notifying_change;

public class without_existing_watcher : given.a_reducer_observers
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    MyReadModel _model;
    ReadModelKey _modelKey;
    EventStoreNamespaceName _namespace;
    Exception _result;

    void Establish()
    {
        _model = new MyReadModel { Name = "Test" };
        _modelKey = "test-key";
        _namespace = "test-namespace";
    }

    void Because() => _result = Catch.Exception(() => _observers.NotifyChange(_namespace, _modelKey, _model, false));

    [Fact] void should_not_throw() => _result.ShouldBeNull();
}
