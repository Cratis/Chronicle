// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_DecimalBulkIdentity.given;

public class a_decimal_bulk_sink : for_Sink.given.a_sink_observing_a_container
{
    protected Key _original = new(1.0m, ArrayIndexers.NoIndexers);
    protected Key _equivalent = new(1.00m, ArrayIndexers.NoIndexers);
    protected ExpandoObject? _answer;
    protected int _reads;

    async Task Establish()
    {
        _converter.ToBsonValue(Arg.Any<Key>()).Returns(call => new BsonDocument("amount", new BsonDecimal128((decimal)call.Arg<Key>().Value)));
        _changesetConverter.ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>())
            .Returns(new UpdateDefinitionAndArrayFilters(Builders<BsonDocument>.Update.Set("count", 2), [], true));
        _primaryId = Guid.NewGuid();
        _documents = [new BsonDocument { { "_id", new BsonDocument("amount", new BsonDecimal128(1.000m)) }, { WellKnownProperties.LastHandledEventSequenceNumber, 42L } }];
        _duringFind = () => _reads++;
        await _sink.BeginBulk();
    }

    protected static Changeset<AppendedEvent, ExpandoObject> Changes()
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = 2;
        var changes = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changes.ReplaceState(state, [new PropertyDifference("count", null, 2)]);
        return changes;
    }
}
