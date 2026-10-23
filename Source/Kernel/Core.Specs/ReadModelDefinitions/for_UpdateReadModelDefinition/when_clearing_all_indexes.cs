// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModelDefinitions.for_UpdateReadModelDefinition;

public class when_clearing_all_indexes : Specification
{
    IReadModels _service;
    UpdateDefinitionRequest _request;

    void Establish()
    {
        _service = Substitute.For<IReadModels>();
        _service.When(service => service.UpdateDefinition(Arg.Any<UpdateDefinitionRequest>())).Do(call => _request = call.Arg<UpdateDefinitionRequest>());
    }

    async Task Because() => await new UpdateReadModelDefinition("store", "model", "models", 1, "{}", []) { IndexDefinitions = [] }.Handle(_service);

    [Fact] void should_send_an_empty_set() => _request.ReadModel.Indexes.ShouldBeEmpty();
    [Fact] void should_distinguish_the_update_from_a_legacy_empty_list() => _request.ReplaceIndexes.ShouldBeTrue();
}
