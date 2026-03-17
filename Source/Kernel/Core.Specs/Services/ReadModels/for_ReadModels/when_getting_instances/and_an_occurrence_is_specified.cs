// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying that specifying an occurrence passes it through to the sink.
/// </summary>
public class and_an_occurrence_is_specified : given.all_dependencies
{
    const string OccurrenceName = "replay-2026-03-01";
    GetInstancesResponse _result;

    void Establish()
    {
        var instance = new ExpandoObject();
        ((IDictionary<string, object?>)instance)["Status"] = "active";

        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([instance], 3));
    }

    async Task Because() => _result = await _service.GetInstances(new GetInstancesRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Occurrence = OccurrenceName,
        Page = 0,
        PageSize = 10
    });

    [Fact] void should_pass_the_occurrence_to_sink() =>
        _sink.Received(1).GetInstances(
            Arg.Is<Concepts.ReadModels.ReadModelContainerName?>(o => o != null && o.Value == OccurrenceName),
            0,
            10);
    [Fact] void should_return_the_instances() => _result.Instances.Count.ShouldEqual(1);
    [Fact] void should_return_total_count() => _result.TotalCount.ShouldEqual(3);
}
