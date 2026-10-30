// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexes;

public class when_a_composite_has_one_collection_path : Specification
{
    [CompositeIndex("Lines[].Sku", nameof(Status), IsDescending = new[] { false, true })]
    record Model(Line[] Lines, string Status);
    record Line(string Sku);

    List<IndexDefinition> _indexes;

    void Because() => _indexes = new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(Model));

    [Fact] void should_accept_one_collection_element_path() => _indexes[0].Properties.Select(property => property.Path).SequenceEqual(["Lines.Sku", "Status"]).ShouldBeTrue();
    [Fact] void should_keep_the_scalar_direction() => _indexes[0].Properties[1].Descending.ShouldBeTrue();
}
