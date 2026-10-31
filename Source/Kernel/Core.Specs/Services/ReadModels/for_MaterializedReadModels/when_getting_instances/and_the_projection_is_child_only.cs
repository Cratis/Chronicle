// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_getting_instances;

public class and_the_projection_is_child_only : for_ReadModels.given.all_dependencies
{
    void Establish()
    {
        _readBindings.Resolve("test-store", "test-namespace", _readModelDefinition, (ReadModelContainerName)"requested-occurrence")
            .Returns(new ReadModelReadBinding(_sink, "bound-occurrence", ReadModelInstanceVisibility.IncludeUninitialized));
        _sink.GetInstances((ReadModelContainerName)"bound-occurrence", 10, 10, ReadModelInstanceVisibility.IncludeUninitialized).Returns(new ReadModelInstances([], 0));
    }

    async Task Because()
    {
        var service = new MaterializedReadModels(_grainFactory, _readBindings, _complianceHelper, NullLogger<MaterializedReadModels>.Instance);
        await service.GetInstances(new()
        {
            EventStore = "test-store",
            Namespace = "test-namespace",
            ReadModel = _readModelDefinition.Identifier,
            Occurrence = "requested-occurrence",
            Page = 1,
            PageSize = 10
        });
    }

    [Fact] void should_resolve_once_with_the_requested_occurrence() => _readBindings.Received(1).Resolve("test-store", "test-namespace", _readModelDefinition, (ReadModelContainerName)"requested-occurrence");
    [Fact] void should_read_the_bound_destination() => _sink.Received(1).GetInstances((ReadModelContainerName)"bound-occurrence", 10, 10, ReadModelInstanceVisibility.IncludeUninitialized);
}
