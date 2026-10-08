// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventEntryConverter;

public class when_reading_unsigned_64_bit_content : Specification
{
    IDictionary<string, object?> _result;

    void Because() => _result = EventEntryConverter.GetContentForGeneration(
        new EventEntry { Content = """{"1":{"value":18446744073709551615,"values":[18446744073709551615],"small":9223372036854775807}}""" }, 1);

    [Fact] void should_preserve_the_maximum_value() => _result["value"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_preserve_array_elements() => ((object[])_result["values"]!)[0].ShouldEqual(ulong.MaxValue);
    [Fact] void should_keep_existing_signed_values() => _result["small"].ShouldEqual(long.MaxValue);
}
