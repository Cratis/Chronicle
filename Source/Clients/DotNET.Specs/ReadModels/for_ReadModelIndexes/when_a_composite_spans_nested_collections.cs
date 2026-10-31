// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexes;

public class when_a_composite_spans_nested_collections : Specification
{
    [CompositeIndex("Orders[].Lines[].Sku", "Orders[].Name")]
    record Model(Order[] Orders);
    record Order(string Name, Line[] Lines);
    record Line(string Sku);

    List<IndexDefinition> _indexes;

    void Because() => _indexes = new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(Model));

    [Fact] void should_accept_the_nested_chain() => _indexes[0].Properties.Select(property => property.Path).SequenceEqual(["Orders.Lines.Sku", "Orders.Name"]).ShouldBeTrue();
}
