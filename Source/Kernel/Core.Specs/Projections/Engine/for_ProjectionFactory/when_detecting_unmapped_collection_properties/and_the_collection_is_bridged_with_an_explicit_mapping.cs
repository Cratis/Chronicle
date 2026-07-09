// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_detecting_unmapped_collection_properties;

/// <summary>
/// The explicit mapping is what a <c>[SetFrom&lt;E&gt;(nameof(E.Notes))]</c> on the child property produces —
/// the read-model <c>annotations</c> property mapped from the differently named event <c>notes</c> list.
/// </summary>
public class and_the_collection_is_bridged_with_an_explicit_mapping : given.a_read_model_with_a_bulk_list_collection
{
    IReadOnlyList<UnmappedCollectionProperty> _result;

    void Because() => _result = ProjectionFactory.FindUnmappedCollectionProperties(
        ProjectionFrom(new Dictionary<PropertyPath, string> { [new PropertyPath("annotations")] = "notes" }),
        _readModelSchema,
        AutoMap.Enabled,
        new HashSet<string>(),
        EventWithListNamed("notes"));

    [Fact] void should_not_flag_the_bridged_collection() => _result.ShouldBeEmpty();
}
