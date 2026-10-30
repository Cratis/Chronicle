// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering;

public class with_composites_sharing_an_array : Specification
{
    [CompositeIndex("Lines.Sku", "Lines.Qty")]
    record Model(List<Line> Lines);
    [CompositeIndex(nameof(Sku), nameof(Qty))]
    record Line(string Sku, int Qty);

    Contracts.ReadModels.IndexDefinition[] _indexes;

    void Because() => _indexes = new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(Model)).ToArray();

    [Fact] void should_coalesce_parent_and_element_declarations() => _indexes.Length.ShouldEqual(1);
    [Fact] void should_register_both_keys_from_the_same_array() => _indexes[0].Properties.Select(property => property.Path).ShouldEqual(["Lines.Sku", "Lines.Qty"]);
}
