// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_restoring_non_clr_decimals : Specification
{
    JsonObject _read;

    void Because() => _read = EventContentBson.ToJsonObject(new BsonDocument
    {
        ["large"] = new BsonDecimal128(Decimal128.Parse("1E+100")),
        ["nan"] = new BsonDecimal128(Decimal128.Parse("NaN")),
        ["positiveInfinity"] = new BsonDecimal128(Decimal128.PositiveInfinity),
        ["negativeInfinity"] = new BsonDecimal128(Decimal128.NegativeInfinity)
    });

    [Fact] void should_preserve_out_of_range_values_as_text() => _read["large"]!.GetValue<string>().ShouldEqual(Decimal128.Parse("1E+100").ToString());
    [Fact] void should_preserve_nan_as_text() => _read["nan"]!.GetValue<string>().ShouldEqual("NaN");
    [Fact] void should_preserve_positive_infinity_as_text() => _read["positiveInfinity"]!.GetValue<string>().ShouldEqual("Infinity");
    [Fact] void should_preserve_negative_infinity_as_text() => _read["negativeInfinity"]!.GetValue<string>().ShouldEqual("-Infinity");
}
