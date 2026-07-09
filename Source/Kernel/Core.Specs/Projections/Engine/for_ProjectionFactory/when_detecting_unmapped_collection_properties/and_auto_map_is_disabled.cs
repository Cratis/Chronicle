// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_detecting_unmapped_collection_properties;

/// <summary>
/// With AutoMap disabled the developer has taken over mapping entirely, so an unmapped collection is a
/// deliberate choice rather than a silent failure and must not be flagged.
/// </summary>
public class and_auto_map_is_disabled : given.a_read_model_with_a_bulk_list_collection
{
    IReadOnlyList<UnmappedCollectionProperty> _result;

    void Because() => _result = ProjectionFactory.FindUnmappedCollectionProperties(
        ProjectionFrom(),
        _readModelSchema,
        AutoMap.Disabled,
        new HashSet<string>(),
        EventWithListNamed("notes"));

    [Fact] void should_not_flag_any_property() => _result.ShouldBeEmpty();
}
