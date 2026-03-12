// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying that instances are serialized to JSON strings in the response.
/// </summary>
public class and_there_are_multiple_instances : given.all_dependencies
{
    GetInstancesResponse _result;
    ExpandoObject _firstInstance;
    ExpandoObject _secondInstance;

    void Establish()
    {
        _firstInstance = new ExpandoObject();
        ((IDictionary<string, object?>)_firstInstance)["Name"] = "Alice";

        _secondInstance = new ExpandoObject();
        ((IDictionary<string, object?>)_secondInstance)["Name"] = "Bob";

        _sink.GetInstances(Arg.Any<Concepts.ReadModels.ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([_firstInstance, _secondInstance], 2));
    }

    async Task Because() => _result = await _service.GetInstances(new GetInstancesRequest
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "test-read-model",
        Page = 0,
        PageSize = 20
    });

    [Fact] void should_return_two_instances() => _result.Instances.Count().ShouldEqual(2);
    [Fact] void should_serialize_first_instance_as_json() =>
        JsonSerializer.Deserialize<JsonElement>(_result.Instances.First()).GetProperty("Name").GetString().ShouldEqual("Alice");
    [Fact] void should_serialize_second_instance_as_json() =>
        JsonSerializer.Deserialize<JsonElement>(_result.Instances.Last()).GetProperty("Name").GetString().ShouldEqual("Bob");
    [Fact] void should_return_total_count() => _result.TotalCount.ShouldEqual(2);
}
