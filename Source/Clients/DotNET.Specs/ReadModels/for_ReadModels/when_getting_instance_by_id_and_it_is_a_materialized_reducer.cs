// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_getting_instance_by_id_and_it_is_a_materialized_reducer : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    ReadModelKey _key;
    MyReadModel _expectedModel;
    object _result = null!;
    Contracts.ReadModels.IReadModels _readModelsService = null!;
    GetInstanceByKeyRequest _capturedRequest = null!;

    void Establish()
    {
        _key = "test-key";
        _expectedModel = new MyReadModel { Name = "FromMaterializedStore" };

        _projections.HasFor(typeof(MyReadModel)).Returns(false);
        _reducers.HasFor(typeof(MyReadModel)).Returns(true);

        var handler = Substitute.For<IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequenceId.Log);
        _reducers.GetHandlerForReadModelType(typeof(MyReadModel)).Returns(handler);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
        _readModelsService.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>())
            .Returns(new GetInstanceByKeyResponse { ReadModel = JsonSerializer.Serialize(_expectedModel) });
        _readModelsService.When(_ => _.GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>()))
            .Do(_ => _capturedRequest = _.Arg<GetInstanceByKeyRequest>());
    }

    async Task Because() => _result = await _readModels.GetInstanceById(typeof(MyReadModel), _key);

    [Fact] void should_get_the_instance_from_the_kernel() => _readModelsService.Received(1).GetInstanceByKey(Arg.Any<GetInstanceByKeyRequest>());
    [Fact] void should_not_reduce_in_process() => _reducers.DidNotReceive().GetInstanceById(typeof(MyReadModel), Arg.Any<ReadModelKey>());
    [Fact] void should_ask_for_the_event_sequence_the_reducer_reduces_from() => _capturedRequest.EventSequenceId.ShouldEqual(EventSequenceId.Log.Value);
    [Fact] void should_use_the_key() => _capturedRequest.ReadModelKey.ShouldEqual(_key.Value);
    [Fact] void should_return_the_materialized_instance() => ((MyReadModel)_result).Name.ShouldEqual(_expectedModel.Name);
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
