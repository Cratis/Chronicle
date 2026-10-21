// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_reading_decimal_content_without_a_schema : Specification
{
    IDictionary<string, object?> _result;

    void Because() => _result = EventEntryConverter.GetContentForGeneration(new EventEntry { Content = """{"1":{"amount":193.58,"precise":1234567890.123456789012345678}}""" }, 1);

    [Fact] void should_preserve_amount_bits() => decimal.GetBits((decimal)_result["amount"]!).ShouldEqual(decimal.GetBits(193.58m));
    [Fact] void should_preserve_all_significant_digits() => decimal.GetBits((decimal)_result["precise"]!).ShouldEqual(decimal.GetBits(1234567890.123456789012345678m));
}
