// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_detecting_unmapped_collection_properties;

/// <summary>
/// Only collections silently project empty; an unmapped scalar is routinely filled by another event, so the
/// detector must not flag scalar properties whose name does not match the event.
/// </summary>
public class and_a_mismatched_scalar_property_is_present : given.a_read_model_with_a_bulk_list_collection
{
    IReadOnlyList<UnmappedCollectionProperty> _result;

    void Because() => _result = ProjectionFactory.FindUnmappedCollectionProperties(
        ProjectionFrom(),
        _readModelSchema,
        AutoMap.Enabled,
        new HashSet<string>(),
        EventOnlyWithScalar());

    [Fact] void should_not_flag_the_scalar_property() => _result.ShouldBeEmpty();

    static IEnumerable<EventTypeSchema> EventOnlyWithScalar() =>
    [
        new EventTypeSchema(
            _lineAdded,
            EventTypeOwner.Client,
            EventTypeSource.User,
            JsonSchema.FromJson(
                """
                {
                  "type": "object",
                  "properties": {
                    "annotations": { "type": "array", "items": { "type": "object" } },
                    "summary": { "type": "string" }
                  }
                }
                """))
    ];
}
