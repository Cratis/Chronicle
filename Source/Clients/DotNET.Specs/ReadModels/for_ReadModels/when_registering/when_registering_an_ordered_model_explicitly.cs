// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class when_registering_an_ordered_model_explicitly : given.an_indexed_read_model
{
    async Task Because() => await _readModels.Register<MyReadModel>();

    [Fact] void should_register_ordered_properties() => _request.ReadModels[0].Indexes.Single(index => index.PropertyPath == "Region").Properties.Select(property => property.Path).SequenceEqual(["Region", "Created"]).ShouldBeTrue();
    [Fact] void should_keep_the_descending_flag() => _request.ReadModels[0].Indexes.Single(index => index.PropertyPath == "Region").Properties[1].Descending.ShouldBeTrue();
}
