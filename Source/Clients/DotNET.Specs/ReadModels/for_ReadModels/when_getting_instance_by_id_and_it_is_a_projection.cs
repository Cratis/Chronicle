// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;

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
    Contracts.ReadModels.IReadModels _readModelsService;
    GetInstanceByKeyRequest _capturedRequest;

    void Establish()
    {
        _key = "test-key";
        _expectedModel = new MyReadModel { Name = "Test" };

        _projections.HasFor(typeof(MyReadModel)).Returns(true);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);

        var json = JsonSerializer.Serialize(_expectedModel);
        _readModelsService.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>())
            .Returns(new GetInstanceByKeyResponse { ReadModel = json });
        _readModelsService.When(_ => _.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>()))
            .Do(_ => _capturedRequest = _.Arg<GetInstanceByKeyRequest>());
    }

    async Task Because() => _result = await _readModels.GetInstanceById(typeof(MyReadModel), _key);

    [Fact] void should_call_read_models_service() => _readModelsService.Received(1).GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>());
    [Fact] void should_use_correct_event_store() => _capturedRequest.EventStore.ShouldEqual(_eventStore.Name.Value);
    [Fact] void should_use_correct_namespace() => _capturedRequest.Namespace.ShouldEqual(_eventStore.Namespace.Value);
    [Fact] void should_use_correct_read_model_identifier() => _capturedRequest.ReadModelIdentifier.ShouldEqual(typeof(MyReadModel).GetReadModelIdentifier().Value);
    [Fact] void should_use_correct_key() => _capturedRequest.ReadModelKey.ShouldEqual(_key.Value);
    [Fact] void should_return_the_model() => ((MyReadModel)_result).Name.ShouldEqual(_expectedModel.Name);
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
