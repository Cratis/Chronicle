// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions;

public class when_converting_time_span_to_bson_value : Specification
{
    TimeSpan _timeSpan;
    BsonValue _result;

    void Establish() => _timeSpan = new TimeSpan(1, 2, 3, 4, 5);

    void Because() => _result = _timeSpan.ToBsonValue();

    [Fact] void should_return_bson_string() => _result.ShouldBeOfExactType<BsonString>();
    [Fact] void should_store_time_span_as_string() => _result.AsString.ShouldEqual(_timeSpan.ToString());
}
