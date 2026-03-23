// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_instances;

/// <summary>
/// Spec verifying that an empty occurrence string is treated as null (no occurrence filter).
/// </summary>
public class and_occurrence_is_empty_string : given.all_dependencies
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
        Occurrence = string.Empty,
        Page = 0,
        PageSize = 20
    });

    [Fact] void should_pass_null_occurrence_to_sink() =>
        _sink.Received(1).GetInstances(null, 0, 20);
}
