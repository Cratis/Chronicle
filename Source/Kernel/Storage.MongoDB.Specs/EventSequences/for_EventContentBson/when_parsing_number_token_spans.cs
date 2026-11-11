// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventContentBson;

public class when_parsing_number_token_spans : Specification
{
    readonly string[] _literals =
    [
        "0", "-0", "42", "-42", "9223372036854775807", "-9223372036854775808",
        "9223372036854775808", "18446744073709551615", "18446744073709551616",
        "0.1", "0.10000000000000001", "0.1234567890123456789012345678", "1.00",
        "1e2", "1E+02", "1e-28", "-0.0", "1e-100", "1e100", "1.7976931348623157e308"
    ];
    readonly List<BsonDocument> _actual = [];
    readonly List<BsonDocument> _expected = [];

    void Because()
    {
        foreach (var literal in _literals)
        {
            var content = "{\"value\":" + literal + "}";
            _actual.Add(EventContentBson.FromJson(content));
            _expected.Add(BsonDocument.Parse(RequiresDecimal(literal)
                ? "{\"value\":{\"$numberDecimal\":\"" + literal + "\"}}"
                : content));
        }
    }

    [Fact] void should_preserve_the_string_parser_bson_representation() => _actual.ShouldEqual(_expected);

    [Fact]
    void should_restore_integer_and_decimal_values_exactly()
    {
        JsonNode.Parse(EventContentBson.ToJson(_actual[7]))!["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
        JsonNode.Parse(EventContentBson.ToJson(_actual[11]))!["value"]!.GetValue<decimal>().ShouldEqual(0.1234567890123456789012345678m);
    }

    static bool RequiresDecimal(string literal) =>
        !long.TryParse(literal, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) &&
        decimal.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out _) &&
        (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
         !string.Equals(literal, value.ToString("R", CultureInfo.InvariantCulture), StringComparison.Ordinal));
}
