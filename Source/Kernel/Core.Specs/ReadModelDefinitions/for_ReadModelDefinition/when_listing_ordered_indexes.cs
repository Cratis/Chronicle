// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.ReadModelDefinitions.for_ReadModelDefinition;

public class when_listing_ordered_indexes : Specification
{
    IReadModels _service;
    ReadModelDefinition[] _definitions;

    void Establish()
    {
        _service = Substitute.For<IReadModels>();
        _service.GetDefinitions(Arg.Any<GetDefinitionsRequest>()).Returns(new GetDefinitionsResponse
        {
            ReadModels =
            [
                new()
                {
                    Type = new() { Identifier = "model", Generation = 1 },
                    Indexes = [new() { PropertyPath = "region", Properties = [new() { Path = "region" }, new() { Path = "created", Descending = true }] }]
                }
            ]
        });
    }

    async Task Because() => _definitions = (await ReadModelDefinition.AllReadModelDefinitions(_service, "store")).ToArray();

    [Fact] void should_keep_the_legacy_path_list() => _definitions[0].Indexes.Single().ShouldEqual("region");
    [Fact] void should_return_the_complete_ordered_properties() => _definitions[0].IndexDefinitions.Single().Properties.Select(property => (property.Path, property.Descending)).SequenceEqual([("region", false), ("created", true)]).ShouldBeTrue();
}
