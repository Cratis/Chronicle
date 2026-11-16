// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_empty_and_null_content : Specification
{
    JsonSchema _schema;
    BsonDocument _empty;
    BsonDocument _null;
    BsonDocument _unknown;

    void Establish() => _schema = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":["number","null"],"format":"decimal"}}}""");

    void Because()
    {
        _empty = EventContentBson.FromJson("{}", _schema);
        _null = EventContentBson.FromJson("""{"amount":null}""", _schema);
        _unknown = EventContentBson.FromJson("""{"unknown":1.25}""", _schema);
    }

    [Fact] void should_keep_an_empty_document_empty() => _empty.ElementCount.ShouldEqual(0);
    [Fact] void should_preserve_null_decimal_properties() => _null["amount"].IsBsonNull.ShouldBeTrue();
    [Fact] void should_not_promote_undescribed_numbers() => _unknown["unknown"].BsonType.ShouldEqual(BsonType.Double);
}
