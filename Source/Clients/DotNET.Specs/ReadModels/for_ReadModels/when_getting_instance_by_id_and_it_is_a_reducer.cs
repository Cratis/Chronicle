// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_getting_instance_by_id_and_it_is_a_reducer : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    ReadModelKey _key;
    MyReadModel _expectedModel;
    object _result = null!;
    Contracts.ReadModels.IReadModels _readModelsService = null!;

    void Establish()
    {
        _key = "test-key";
        _expectedModel = new MyReadModel { Name = "FromReducer" };

        _projections.HasFor(typeof(MyReadModel)).Returns(false);
        _reducers.HasFor(typeof(MyReadModel)).Returns(true);
        _reducers.GetInstanceById(typeof(MyReadModel), _key).Returns(_expectedModel);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
    }

    async Task Because() => _result = await _readModels.GetInstanceById(typeof(MyReadModel), _key);

    [Fact] void should_get_instance_from_reducers() => _reducers.Received(1).GetInstanceById(typeof(MyReadModel), _key);
    [Fact] void should_not_call_read_models_service() => _readModelsService.DidNotReceive().GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>());
    [Fact] void should_return_model_from_reducer() => ((MyReadModel)_result).Name.ShouldEqual("FromReducer");
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
