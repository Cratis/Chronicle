// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_looking_up_by_composite_key;

public class and_two_documents_hold_the_parts_in_different_orders : given.a_composite_key_lookup
{
    void Establish() => _documents = [new BsonDocument("_id", _id), new BsonDocument("_id", new BsonDocument(_id.Elements.Reverse()))];
    async Task Because() => _result = await Lookup();
    [Fact] void should_report_ambiguity() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.AmbiguousLegacyKey);
    [Fact] void should_not_choose_an_instance() => _result.Instance.ShouldBeNull();
}
