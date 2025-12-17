// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_getting_instance_by_id_and_it_is_a_projection : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    ReadModelKey _key;
    MyReadModel _expectedModel;
    object _result;

    void Establish()
    {
        _key = "test-key";
        _expectedModel = new MyReadModel { Name = "Test" };

        _projections.HasFor(typeof(MyReadModel)).Returns(true);
        _projections.GetInstanceById(typeof(MyReadModel), _key)
            .Returns(new ProjectionResult(_expectedModel, 1, new Events.EventSequenceNumber(42)));
    }

    async Task Because() => _result = await _readModels.GetInstanceById(typeof(MyReadModel), _key);

    [Fact] void should_check_if_projection_exists() => _projections.Received(1).HasFor(typeof(MyReadModel));
    [Fact] void should_get_instance_from_projection() => _projections.Received(1).GetInstanceById(typeof(MyReadModel), _key);
    [Fact] void should_return_the_model() => _result.ShouldEqual(_expectedModel);
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
