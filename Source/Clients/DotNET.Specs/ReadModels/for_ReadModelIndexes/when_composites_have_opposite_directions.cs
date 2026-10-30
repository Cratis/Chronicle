// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels.for_ReadModelIndexes;

public class when_composites_have_opposite_directions : Specification
{
    [CompositeIndex(nameof(Region), nameof(Name))]
    [CompositeIndex(nameof(Region), nameof(Name), IsDescending = new[] { true, true })]
    record Model(string Region, string Name);

    List<IndexDefinition> _indexes;

    void Because() => _indexes = new ReadModelIndexes(new DefaultNamingPolicy()).For(typeof(Model));

    [Fact] void should_not_collapse_indexes_with_different_directions() => _indexes.Count.ShouldEqual(2);
    [Fact] void should_keep_the_ascending_declaration() => _indexes.ShouldContain(index => index.Properties.All(property => !property.Descending));
    [Fact] void should_keep_the_descending_declaration() => _indexes.ShouldContain(index => index.Properties.All(property => property.Descending));
}
