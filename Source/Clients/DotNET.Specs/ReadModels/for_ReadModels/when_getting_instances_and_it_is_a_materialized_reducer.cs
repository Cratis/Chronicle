// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_getting_instances_and_it_is_a_materialized_reducer : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    IEnumerable<MyReadModel> _result = [];
    Contracts.ReadModels.IReadModels _readModelsService = null!;

    void Establish()
    {
        _projections.HasFor(typeof(MyReadModel)).Returns(false);
        _reducers.HasFor(typeof(MyReadModel)).Returns(true);

        var handler = Substitute.For<IReducerHandler>();
        handler.EventSequenceId.Returns(EventSequenceId.Log);
        _reducers.GetHandlerForReadModelType(typeof(MyReadModel)).Returns(handler);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
        _readModelsService.GetAllInstances(Arg.Any<GetAllInstancesRequest>())
            .Returns(new GetAllInstancesResponse
            {
                Instances =
                [
                    JsonSerializer.Serialize(new MyReadModel { Name = "First" }),
                    JsonSerializer.Serialize(new MyReadModel { Name = "Second" })
                ]
            });
    }

    async Task Because() => _result = await _readModels.GetInstances<MyReadModel>();

    [Fact] void should_get_the_instances_from_the_kernel() => _readModelsService.Received(1).GetAllInstances(Arg.Any<GetAllInstancesRequest>());
    [Fact] void should_not_reduce_in_process() => _reducers.DidNotReceive().GetInstances(typeof(MyReadModel), Arg.Any<EventCount>());
    [Fact] void should_return_all_materialized_instances() => _result.Count().ShouldEqual(2);
    [Fact] void should_return_the_first_instance() => _result.First().Name.ShouldEqual("First");
    [Fact] void should_return_the_second_instance() => _result.Last().Name.ShouldEqual("Second");
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
