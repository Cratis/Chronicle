// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_snapshots_by_id;

public class and_it_is_unknown : given.all_dependencies
{
    public class MyReadModel
    {
        public string Name { get; set; }
    }

    ReadModelKey _key;
    Exception _exception;

    void Establish()
    {
        _key = "test-key";

        _projections.HasFor(typeof(MyReadModel)).Returns(false);
        _reducers.HasReducerFor(typeof(MyReadModel)).Returns(false);
    }

    async Task Because() => _exception = await Catch.Exception(() => _readModels.GetSnapshotsById<MyReadModel>(_key));

    [Fact] void should_check_if_projection_exists() => _projections.Received(1).HasFor(typeof(MyReadModel));
    [Fact] void should_check_if_reducer_exists() => _reducers.Received(1).HasReducerFor(typeof(MyReadModel));
    [Fact] void should_throw_unknown_read_model() => _exception.ShouldBeOfExactType<UnknownReadModel>();
    [Fact] void should_include_read_model_type_in_exception() => (_exception as UnknownReadModel)!.ReadModelType.ShouldEqual(typeof(MyReadModel));
}
