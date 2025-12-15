// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_BsonValueExtensions.when_converting_to_time_span;

public class from_bson_string : Specification
{
    TimeSpan _expected;
    BsonValue _bsonValue;
    object? _result;

    void Establish()
    {
        _expected = new TimeSpan(1, 2, 3, 4, 5);
        _bsonValue = new BsonString(_expected.ToString());
    }

    void Because() => _result = _bsonValue.ToTargetType(typeof(TimeSpan));

    [Fact] void should_return_time_span() => _result.ShouldBeOfExactType<TimeSpan>();
    [Fact] void should_have_correct_value() => ((TimeSpan)_result!).ShouldEqual(_expected);
}
