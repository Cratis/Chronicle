// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_watching;

public class and_changeset_is_emitted : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    Subject<ProjectionChangeset<MyReadModel>> _projectionObservable;
    ReadModelChangeset<MyReadModel> _receivedChangeset;
    MyReadModel _model;
    ReadModelKey _modelKey;
    EventStoreNamespaceName _namespace;

    void Establish()
    {
        _model = new MyReadModel { Name = "Test" };
        _modelKey = "test-key";
        _namespace = "test-namespace";

        _projectionObservable = new Subject<ProjectionChangeset<MyReadModel>>();
        _projections.HasFor(typeof(MyReadModel)).Returns(true);
        _projections.Watch<MyReadModel>().Returns(_projectionObservable);

        var observable = _readModels.Watch<MyReadModel>();
        observable.Subscribe(changeset => _receivedChangeset = changeset);
    }

    void Because() => _projectionObservable.OnNext(new ProjectionChangeset<MyReadModel>(_namespace, _modelKey, _model, false));

    [Fact] void should_map_namespace() => _receivedChangeset.Namespace.ShouldEqual(_namespace);
    [Fact] void should_map_model_key() => _receivedChangeset.ModelKey.ShouldEqual(_modelKey);
    [Fact] void should_map_read_model() => _receivedChangeset.ReadModel.ShouldEqual(_model);
    [Fact] void should_not_be_removed() => _receivedChangeset.Removed.ShouldBeFalse();
}
