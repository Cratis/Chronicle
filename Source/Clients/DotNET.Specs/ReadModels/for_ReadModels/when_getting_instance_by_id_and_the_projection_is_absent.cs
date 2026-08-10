// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_getting_instance_by_id_and_the_projection_is_absent : given.all_dependencies
{
    class MyReadModel;

    ReadModelKey _key;
    object _result;
    Contracts.ReadModels.IReadModels _readModelsService;

    void Establish()
    {
        _key = "test-key";
        _projections.HasFor(typeof(MyReadModel)).Returns(true);
        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
        _readModelsService.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>())
            .Returns(new GetInstanceByKeyResponse { ReadModel = "null" });
    }

    async Task Because() => _result = await _readModels.GetInstanceById(typeof(MyReadModel), _key);

    [Fact] void should_call_read_models_service() => _readModelsService.Received(1).GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>());
    [Fact] void should_return_null() => _result.ShouldBeNull();
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
