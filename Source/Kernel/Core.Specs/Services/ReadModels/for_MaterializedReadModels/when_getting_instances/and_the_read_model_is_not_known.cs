// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_getting_instances;

public class and_the_read_model_is_not_known : for_ReadModels.given.all_dependencies
{
    MaterializedReadModels _materializedService;
    Exception _result;

    void Establish()
    {
        _readModel.GetDefinition().Returns(default(Concepts.ReadModels.ReadModelDefinition)!);
        _materializedService = new(_grainFactory, _storage, _complianceHelper);
    }

    async Task Because() => _result = await Catch.Exception(() => _materializedService.GetInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModel = "unknown-read-model"
    }));

    [Fact] void should_report_the_missing_definition() => _result.ShouldBeOfExactType<ReadModelNotFound>();
    [Fact] void should_not_resolve_a_sink() => _sinks.DidNotReceiveWithAnyArgs().GetFor(default!);
}
