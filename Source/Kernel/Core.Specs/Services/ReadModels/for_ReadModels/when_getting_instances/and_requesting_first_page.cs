// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying that requesting page 0 (first page, 0-based) passes skip=0 to the sink.
/// </summary>
public class and_requesting_first_page : given.all_dependencies
{
    GetInstancesResponse _result;
    ReadModelInstances _sinkResult;

    void Establish()
    {
        var instance = new ExpandoObject();
        ((IDictionary<string, object?>)instance)["Name"] = "Test";
        _sinkResult = new ReadModelInstances([instance], 5);

        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(_sinkResult);
    }

    async Task Because() => _result = await _service.GetInstances(new GetInstancesRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Page = 0,
        PageSize = 20
    });

    [Fact] void should_pass_zero_skip_to_sink() =>
        _sink.Received(1).GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), 0, 20);
    [Fact] void should_return_the_instances() => _result.Instances.Count().ShouldEqual(1);
    [Fact] void should_return_total_count() => _result.TotalCount.ShouldEqual(5);
    [Fact] void should_echo_back_page() => _result.Page.ShouldEqual(0);
    [Fact] void should_echo_back_page_size() => _result.PageSize.ShouldEqual(20);
}
