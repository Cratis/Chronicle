// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_detecting_unmapped_collection_properties;

/// <summary>
/// A property the developer explicitly opted out of AutoMap is unmapped on purpose, so it must not be flagged.
/// </summary>
public class and_the_collection_is_marked_no_auto_map : given.a_read_model_with_a_bulk_list_collection
{
    IReadOnlyList<UnmappedCollectionProperty> _result;

    void Because() => _result = ProjectionFactory.FindUnmappedCollectionProperties(
        ProjectionFrom(),
        _readModelSchema,
        AutoMap.Enabled,
        new HashSet<string> { "annotations" },
        EventWithListNamed("notes"));

    [Fact] void should_not_flag_the_opted_out_collection() => _result.ShouldBeEmpty();
}
