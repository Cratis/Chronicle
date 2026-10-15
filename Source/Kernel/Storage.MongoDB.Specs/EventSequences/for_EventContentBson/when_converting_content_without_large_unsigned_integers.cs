// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_converting_content_without_large_unsigned_integers : Specification
{
    const string Json = """
        {"small":1,"signed":9223372036854775807,"negative":-1,"decimal":123.456,
         "double":1.25e30,"date":"2026-10-06T12:34:56Z","bsonDate":{"$date":"2026-10-06T12:34:56Z"},
         "bsonDecimal":{"$numberDecimal":"123.456"},"bsonDouble":{"$numberDouble":"1.25"},
         "null":null,"true":true,"false":false,"text":"18446744073709551615",
         "escaped":"quoted \"18446744073709551615\"","nested":{"value":42},
         "array":["x",1,1.5,null,true,{"a":[1]}]}
        """;
    BsonDocument _expected;
    BsonDocument _result;

    void Establish() => _expected = BsonDocument.Parse(Json);

    void Because() => _result = EventContentBson.FromJson(Json);

    [Fact] void should_match_the_existing_bson_parser() => _result.ShouldEqual(_expected);
    [Fact] void should_preserve_the_exact_bson_encoding() => _result.ToBson().ShouldEqual(_expected.ToBson());
}
