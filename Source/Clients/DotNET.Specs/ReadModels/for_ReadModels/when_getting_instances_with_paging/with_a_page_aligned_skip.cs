// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_getting_instances_with_paging;

public class with_a_page_aligned_skip : given.instances_in_the_sink
{
    IEnumerable<PagedReadModel> _result = [];

    async Task Because() => _result = await _readModels.Materialized.GetInstances<PagedReadModel>(10, 5);

    [Fact] void should_return_the_expected_page() => _result.Count().ShouldEqual(5);

    [Fact] void should_return_the_page_in_order() =>
        _result.Select(instance => instance.Name).SequenceEqual(Enumerable.Range(10, 5).Select(index => $"Item{index}")).ShouldBeTrue();

    [Fact] void should_request_the_aligned_page_without_over_fetching() =>
        _materializedReadModelsService.Received(1).GetInstances(Arg.Is<GetInstancesRequest>(request => request.Page == 2 && request.PageSize == 5));
}
