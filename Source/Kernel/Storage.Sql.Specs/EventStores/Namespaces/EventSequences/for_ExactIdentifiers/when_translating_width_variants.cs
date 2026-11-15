// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers;

public class when_translating_width_variants : given.a_sql_server_query
{
    string[] _first;
    string[] _second;

    void Because()
    {
        _first = Translate("Alice");
        _second = Translate("Ａlice");
    }

    [Fact] void should_preserve_the_full_width_parameter() => _second.All(_ => _.Contains("= N'Ａlice'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_narrow_parameter() => _first.All(_ => _.Contains("= N'Alice'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_apply_binary_collation_to_both_widths() => _first.Concat(_second).All(_ => _.Contains("COLLATE Latin1_General_100_BIN2", StringComparison.Ordinal)).ShouldBeTrue();
}
