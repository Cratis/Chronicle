// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexes;

public class when_a_collection_element_is_a_value_type : Specification
{
    record Model(List<Line> Lines);
    record struct Line([property: Index] string Sku);

    List<IndexDefinition> _indexes;

    void Because() => _indexes = new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(Model));

    [Fact] void should_keep_collecting_indexes_on_value_type_elements() => _indexes.Single().PropertyPath.ShouldEqual("Lines.Sku");
}
