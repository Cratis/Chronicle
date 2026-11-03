// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_DecimalBulkIdentity;

public class when_redelivering_to_an_equivalent_nan_key : given.a_decimal_bulk_sink
{
    async Task Establish()
    {
        _original = new(Decimal128.QNaN, ArrayIndexers.NoIndexers);
        _equivalent = new(Decimal128.FromIEEEBits(0xFE00000000000001, 42), ArrayIndexers.NoIndexers);
        _converter.ToBsonValue(Arg.Any<Key>()).Returns(call => new BsonDocument("amount", new BsonDecimal128((Decimal128)call.Arg<Key>().Value)));
        _documents = [new BsonDocument { { "_id", new BsonDocument("amount", new BsonDecimal128(Decimal128.SNaN)) }, { WellKnownProperties.LastHandledEventSequenceNumber, 42L } }];
        await _sink.PrefetchPage([_original]);
    }

    async Task Because() => await _sink.ApplyChanges(_equivalent, Changes(), 42UL, SinkWriteMode.OnlyWhenAdvancingWatermark);

    [Fact] void should_skip_the_update_using_the_prefetched_watermark() => _changesetConverter.DidNotReceive().ToUpdateDefinition(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_not_reread_the_equivalent_document() => _reads.ShouldEqual(1);
}
