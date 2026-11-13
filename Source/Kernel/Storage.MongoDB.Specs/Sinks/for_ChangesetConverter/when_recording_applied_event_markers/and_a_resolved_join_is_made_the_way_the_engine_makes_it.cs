// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_recording_applied_event_markers;

/// <summary>
/// The engine resolves a join with the key of the read model being written, which is not the joined instance. The guard
/// must compare the stored reference with the join event's source, or every resolved join would be dropped.
/// </summary>
public class and_a_resolved_join_is_made_the_way_the_engine_makes_it : given.a_changeset_converter_recording_markers
{
    const string JoinSource = "customer-c";

    string _json;

    async Task Because()
    {
        var joinEvent = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with
            {
                EventType = new EventType("customer-registered", 1),
                EventSourceId = JoinSource,
                SequenceNumber = 7UL
            }
        };
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), AppendedEvent.Empty(), new ExpandoObject());
        changeset
            .ResolvedJoin(new PropertyPath("customerId"), _key.Value, joinEvent, ArrayIndexers.NoIndexers)
            .SetProperties([PropertyMappers.FromEventValueProvider("customerName", _ => "Ada")], ArrayIndexers.NoIndexers);

        var result = await Convert(changeset.Changes, Context());
        _json = result.UpdateDefinition.Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)).ToJson();
    }

    [Fact] void should_compare_the_reference_with_the_join_event_source() => _json.ShouldContain($"{{ \"$literal\" : \"{JoinSource}\" }}");
    [Fact] void should_not_compare_the_reference_with_the_read_model_key() => _json.ShouldNotContain($"{{ \"$literal\" : \"{_key.Value}\" }}");
}
