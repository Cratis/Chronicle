// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_detecting_unmapped_collection_properties;

public class and_the_collection_name_does_not_match_the_event : given.a_read_model_with_a_bulk_list_collection
{
    IReadOnlyList<UnmappedCollectionProperty> _result;

    void Because() => _result = ProjectionFactory.FindUnmappedCollectionProperties(
        ProjectionFrom(),
        _readModelSchema,
        AutoMap.Enabled,
        new HashSet<string>(),
        EventWithListNamed("notes"));

    [Fact] void should_flag_the_unmapped_collection() => _result.Count.ShouldEqual(1);
    [Fact] void should_name_the_collection_property() => _result[0].Property.ShouldEqual("annotations");
    [Fact] void should_name_the_source_event() => _result[0].EventTypes.ShouldEqual("NotedLineAdded");
}
