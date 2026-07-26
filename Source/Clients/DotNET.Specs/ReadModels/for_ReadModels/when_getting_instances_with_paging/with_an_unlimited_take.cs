// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_instances_with_paging;

public class with_an_unlimited_take : given.instances_in_the_sink
{
    IEnumerable<PagedReadModel> _result = [];

    async Task Because() => _result = await _readModels.Materialized.GetInstances<PagedReadModel>(0, InstanceCount.Unlimited);

    [Fact] void should_return_all_instances() => _result.Count().ShouldEqual(TotalInstances);

    [Fact] void should_request_an_unlimited_page_from_the_start() =>
        _materializedReadModelsService.Received(1).GetInstances(Arg.Is<GetInstancesRequest>(request => request.Page == 0 && request.PageSize == int.MaxValue));
}
