// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying that requesting page 2 (third page, 0-based) with page size 5 calculates skip as 2*5 = 10.
/// </summary>
public class and_requesting_third_page : given.all_dependencies
{
    GetInstancesResponse _result;

    void Establish()
    {
        var instance1 = new ExpandoObject();
        ((IDictionary<string, object?>)instance1)["Id"] = "item-11";
        var instance2 = new ExpandoObject();
        ((IDictionary<string, object?>)instance2)["Id"] = "item-12";

        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([instance1, instance2], 12));
    }

    async Task Because() => _result = await _service.GetInstances(new GetInstancesRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Page = 2,
        PageSize = 5
    });

    [Fact] void should_skip_ten_records() =>
        _sink.Received(1).GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), 10, 5);
    [Fact] void should_return_the_instances() => _result.Instances.Count().ShouldEqual(2);
    [Fact] void should_return_total_count() => _result.TotalCount.ShouldEqual(12);
    [Fact] void should_echo_back_page() => _result.Page.ShouldEqual(2);
    [Fact] void should_echo_back_page_size() => _result.PageSize.ShouldEqual(5);
}
