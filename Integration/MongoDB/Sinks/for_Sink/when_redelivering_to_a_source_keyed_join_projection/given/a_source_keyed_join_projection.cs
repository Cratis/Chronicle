// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection.given;

public class a_source_keyed_join_projection(ChronicleInProcessFixture fixture) : when_applying_a_direct_change_and_a_join_to_the_same_property.given.a_sink_over_real_mongo(fixture)
{
    protected const string JoinedStream = "b1";
    protected const string Applied = "__applied";

    async Task Establish() =>
        await _documents.UpdateManyAsync(FilterDefinition<BsonDocument>.Empty, Builders<BsonDocument>.Update.Set("joinId", JoinedStream));

    protected static AppendedEvent JoinEvent(ulong sequenceNumber, string eventTypeId = "joined") => EventOf(eventTypeId, JoinedStream, sequenceNumber);

    protected static AppendedEvent OwnEvent(ulong sequenceNumber, string eventSourceId = "root") => EventOf("own", eventSourceId, sequenceNumber);

    protected static Joined JoinOf(params (string Property, int Value)[] values) =>
        new(new ExpandoObject(), JoinedStream, new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(values)]);

    protected static ResolvedJoin ResolvedOf(AppendedEvent source, params (string Property, int Value)[] values) =>
        new(new ExpandoObject(), JoinedStream, new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(values)], source);

    protected static PropertiesChanged<ExpandoObject> DirectText(params (string Property, string Value)[] values) =>
        new(new ExpandoObject(), values.Select(_ => new PropertyDifference(new PropertyPath(_.Property), null, _.Value)).ToArray());

    protected static ResolvedJoin ResolvedFor(string key, AppendedEvent source, params (string Property, int Value)[] values) =>
        new(new ExpandoObject(), key, new PropertyPath("joinId"), ArrayIndexers.NoIndexers, [Direct(values)], source);

    protected static BsonDocument SlotOf(BsonDocument document) => document[Applied]["j"].AsBsonDocument.Single().Value.AsBsonDocument;

    protected Task Apply(Sink sink, AppendedEvent incoming, params Change[] changes) =>
        sink.ApplyChanges(RootKey, ChangesetOf(incoming, changes), incoming.Context.SequenceNumber, SinkWriteMode.OncePerSourceEvent);
}
