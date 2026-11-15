// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers;

public class when_translating_trailing_space_variants : given.a_sql_server_query
{
    string[] _first;
    string[] _second;

    void Because()
    {
        _first = Translate("Alice");
        _second = Translate("Alice ");
    }

    [Fact] void should_preserve_the_trailing_space_parameter() => _second.All(_ => _.Contains("= N'Alice '", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_measure_the_unpadded_value_in_utf16_bytes() => _first.All(_ => _.Contains("= 10;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_measure_the_padded_value_in_utf16_bytes() => _second.All(_ => _.Contains("= 12;", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compare_the_stored_byte_length_for_both_spellings() => _first.Concat(_second).All(_ => _.Contains("DATALENGTH(", StringComparison.Ordinal)).ShouldBeTrue();
}
