// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_getting_instances_and_it_is_a_reducer : given.all_dependencies
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
        _reducers.GetInstances(typeof(MyReadModel), (EventCount)2).Returns(
        [
            new MyReadModel { Name = "First" },
            new MyReadModel { Name = "Second" }
        ]);

        _readModelsService = Substitute.For<Contracts.ReadModels.IReadModels>();
        _services.ReadModels.Returns(_readModelsService);
    }

    async Task Because() => _result = await _readModels.GetInstances<MyReadModel>(2);

    [Fact] void should_get_instances_from_reducers() => _reducers.Received(1).GetInstances(typeof(MyReadModel), (EventCount)2);
    [Fact] void should_not_call_read_models_service() => _readModelsService.DidNotReceive().GetAllInstances(Arg.Any<GetAllInstancesRequest>());
    [Fact] void should_return_all_reducer_instances() => _result.Count().ShouldEqual(2);
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
