// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModelDefinitions.for_UpdateReadModelDefinition;

public class when_sending_ordered_indexes : Specification
{
    IReadModels _service;
    UpdateDefinitionRequest _request;
    UpdateReadModelDefinition _command;

    void Establish()
    {
        _service = Substitute.For<IReadModels>();
        _service.When(service => service.UpdateDefinition(Arg.Any<UpdateDefinitionRequest>())).Do(call => _request = call.Arg<UpdateDefinitionRequest>());
        _command = new("store", "model", "models", 1, "{}", ["old"])
        {
            IndexDefinitions =
            [
                new() { PropertyPath = "region", Properties = [new() { Path = "region" }, new() { Path = "created", Descending = true }] },
                new() { PropertyPath = "name" }
            ]
        };
    }

    async Task Because() => await _command.Handle(_service);

    [Fact] void should_request_a_complete_replacement() => _request.ReplaceIndexes.ShouldBeTrue();
    [Fact] void should_send_the_complete_ordered_definition() => _request.ReadModel.Indexes[0].Properties.Select(property => (property.Path, property.Descending)).SequenceEqual([("region", false), ("created", true)]).ShouldBeTrue();
    [Fact] void should_not_rebuild_the_definition_from_legacy_strings() => _request.ReadModel.Indexes.Any(index => index.PropertyPath == "old").ShouldBeFalse();
    [Fact] void should_express_ascending_single_properties_in_the_structured_update() => _request.ReadModel.Indexes[1].Properties[0].Path.ShouldEqual("name");
}
