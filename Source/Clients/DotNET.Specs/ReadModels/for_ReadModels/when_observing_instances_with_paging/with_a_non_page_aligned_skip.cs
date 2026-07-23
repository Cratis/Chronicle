// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing_instances_with_paging;

public class with_a_non_page_aligned_skip : given.instances_in_the_sink
{
    IEnumerable<PagedReadModel> _emitted = [];

    async Task Because() => _emitted = await _readModels.Materialized.ObserveInstances<PagedReadModel>(5, 10).FirstAsync().ToTask();

    [Fact] void should_emit_the_full_requested_window() => _emitted.Count().ShouldEqual(10);

    [Fact] void should_emit_the_window_in_order() =>
        _emitted.Select(instance => instance.Name).SequenceEqual(Enumerable.Range(5, 10).Select(index => $"Item{index}")).ShouldBeTrue();

    [Fact] void should_request_a_covering_range_from_the_start() =>
        _materializedReadModelsService.Received(1).ObserveInstances(Arg.Is<ObserveInstancesRequest>(request => request.Page == 0 && request.PageSize == 15));
}
