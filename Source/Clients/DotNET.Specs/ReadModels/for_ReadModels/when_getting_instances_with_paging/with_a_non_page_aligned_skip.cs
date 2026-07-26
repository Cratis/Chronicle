// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_instances_with_paging;

public class with_a_non_page_aligned_skip : given.instances_in_the_sink
{
    IEnumerable<PagedReadModel> _result = [];

    async Task Because() => _result = await _readModels.Materialized.GetInstances<PagedReadModel>(5, 10);

    [Fact] void should_return_the_full_requested_window() => _result.Count().ShouldEqual(10);

    [Fact] void should_return_the_window_in_order() =>
        _result.Select(instance => instance.Name).SequenceEqual(Enumerable.Range(5, 10).Select(index => $"Item{index}")).ShouldBeTrue();

    [Fact] void should_request_a_covering_range_from_the_start() =>
        _materializedReadModelsService.Received(1).GetInstances(Arg.Is<GetInstancesRequest>(request => request.Page == 0 && request.PageSize == 15));
}
