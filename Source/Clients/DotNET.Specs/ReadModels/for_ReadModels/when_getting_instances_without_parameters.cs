// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels;

#pragma warning disable CA2263 // Prefer generic overload when type is known
public class when_getting_instances_without_parameters : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    IEnumerable<MyReadModel> _result = [];
    Contracts.ReadModels.IMaterializedReadModels _materializedReadModelsService = null!;
    GetInstancesResponse _serviceResponse = null!;

    void Establish()
    {
        _projections.HasFor(typeof(MyReadModel)).Returns(true);
        _reducers.HasFor(typeof(MyReadModel)).Returns(false);

        _serviceResponse = new GetInstancesResponse
        {
            Instances =
            [
                "{\"Name\":\"First\"}",
                "{\"Name\":\"Second\"}"
            ],
            TotalCount = 2,
            Page = 0,
            PageSize = 50
        };

        _materializedReadModelsService = Substitute.For<Contracts.ReadModels.IMaterializedReadModels>();
        _materializedReadModelsService.GetInstances(Arg.Any<GetInstancesRequest>()).Returns(_serviceResponse);
        _services.MaterializedReadModels.Returns(_materializedReadModelsService);
    }

    async Task Because() => _result = await _readModels.Materialized.GetInstances<MyReadModel>();

    [Fact] void should_call_materialized_read_models_service_with_page_0() =>
        _materializedReadModelsService.Received(1).GetInstances(Arg.Is<GetInstancesRequest>(req => req.Page == 0));

    [Fact] void should_call_materialized_read_models_service_with_default_page_size_50() =>
        _materializedReadModelsService.Received(1).GetInstances(Arg.Is<GetInstancesRequest>(req => req.PageSize == 50));

    [Fact] void should_return_two_instances() => _result.Count().ShouldEqual(2);

    [Fact] void should_return_correct_instances() => _result.First().Name.ShouldEqual("First");
}
#pragma warning restore CA2263 // Prefer generic overload when type is known
