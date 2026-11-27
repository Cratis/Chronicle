// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels;

public class when_the_database_is_unaffected : given.a_database_with_provenance
{
    Contracts.ReadModels.GetInstancesResponse _response;

    void Establish()
    {
        _sinks.GetFor(_readModelDefinition, _readModelDefinition.ContainerName).Returns(_sink);
        dynamic instance = new ExpandoObject();
        instance.Name = "ordinary";
        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ReadModelInstances([(ExpandoObject)instance], 1));
    }

    async Task Because() => _response = await _service.GetInstances(new()
    {
        EventStore = "store",
        Namespace = "namespace",
        ReadModel = "test-read-model",
        PageSize = 10
    });

    [Fact] void should_return_the_materialized_instance() => _response.Instances.Single().ShouldEqual("{\"Name\":\"ordinary\"}");
    [Fact] void should_return_the_stored_count() => _response.TotalCount.ShouldEqual(1L);
    [Fact] async Task should_not_mark_the_database() => (await _marker.IsQuarantined()).ShouldBeFalse();
}
