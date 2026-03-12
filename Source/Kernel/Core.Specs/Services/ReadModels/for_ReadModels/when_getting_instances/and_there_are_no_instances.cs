// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying behavior when the sink returns empty instances.
/// </summary>
public class and_there_are_no_instances : given.all_dependencies
{
    GetInstancesResponse _result;

    void Establish()
    {
        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([], 0));
    }

    async Task Because() => _result = await _service.GetInstances(new GetInstancesRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Page = 0,
        PageSize = 20
    });

    [Fact] void should_return_empty_instances() => _result.Instances.ShouldBeEmpty();
    [Fact] void should_return_zero_total_count() => _result.TotalCount.ShouldEqual(0L);
    [Fact] void should_echo_back_page() => _result.Page.ShouldEqual(0);
    [Fact] void should_echo_back_page_size() => _result.PageSize.ShouldEqual(20);
}
