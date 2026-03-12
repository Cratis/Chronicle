// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying that requesting page 1 (second page, 0-based) correctly calculates skip as 1*pageSize.
/// </summary>
public class and_requesting_second_page : given.all_dependencies
{
    GetInstancesResponse _result;

    void Establish()
    {
        var instance = new ExpandoObject();
        ((IDictionary<string, object?>)instance)["Name"] = "Page2Item";

        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([instance], 25));
    }

    async Task Because() => _result = await _service.GetInstances(new GetInstancesRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Page = 1,
        PageSize = 10
    });

    [Fact] void should_pass_skip_equal_to_page_size() =>
        _sink.Received(1).GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), 10, 10);
    [Fact] void should_return_the_instances() => _result.Instances.Count().ShouldEqual(1);
    [Fact] void should_return_total_count() => _result.TotalCount.ShouldEqual(25);
    [Fact] void should_echo_back_page() => _result.Page.ShouldEqual(1);
}
