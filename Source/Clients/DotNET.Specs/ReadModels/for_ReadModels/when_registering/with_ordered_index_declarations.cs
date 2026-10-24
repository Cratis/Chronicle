// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class with_ordered_index_declarations : given.an_indexed_read_model
{
    async Task Because() => await _readModels.Register();

    [Fact] void should_register_a_mixed_direction_composite() => _request.ReadModels[0].Indexes.Single(index => index.PropertyPath == "Region").Properties.Select(property => (property.Path, property.Descending)).SequenceEqual([("Region", false), ("Created", true)]).ShouldBeTrue();
    [Fact] void should_default_a_composite_to_ascending() => _request.ReadModels[0].Indexes.Single(index => index.PropertyPath == "Address.City" && index.Properties.Count == 2).Properties.All(property => !property.Descending).ShouldBeTrue();
    [Fact] void should_honor_descending_on_a_property() => _request.ReadModels[0].Indexes.Single(index => index.PropertyPath == "Total").Properties[0].Descending.ShouldBeTrue();
    [Fact] void should_honor_descending_on_a_record_parameter() => _request.ReadModels[0].Indexes.Single(index => index.PropertyPath == "Sequence").Properties[0].Descending.ShouldBeTrue();
    [Fact] void should_keep_the_legacy_ascending_fallback() => _request.ReadModels[0].Indexes.Single(index => index.PropertyPath == "Legacy").Properties.ShouldBeEmpty();
    [Fact] void should_keep_nested_property_indexes() => _request.ReadModels[0].Indexes.Count(index => index.PropertyPath == "Address.City").ShouldEqual(2);
}
