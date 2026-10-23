// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModelDefinitions.for_UpdateReadModelDefinition;

public class when_sending_legacy_flat_indexes : Specification
{
    IReadModels _service;
    UpdateDefinitionRequest _request;

    void Establish()
    {
        _service = Substitute.For<IReadModels>();
        _service.When(service => service.UpdateDefinition(Arg.Any<UpdateDefinitionRequest>())).Do(call => _request = call.Arg<UpdateDefinitionRequest>());
    }

    async Task Because() => await new UpdateReadModelDefinition("store", "model", "models", 1, "{}", ["name"]).Handle(_service);

    [Fact] void should_not_request_a_complete_replacement() => _request.ReplaceIndexes.ShouldBeFalse();
    [Fact] void should_keep_the_flat_update_path() => _request.ReadModel.Indexes[0].PropertyPath.ShouldEqual("name");
    [Fact] void should_leave_legacy_merging_enabled() => _request.ReadModel.Indexes[0].Properties.ShouldBeEmpty();
}
