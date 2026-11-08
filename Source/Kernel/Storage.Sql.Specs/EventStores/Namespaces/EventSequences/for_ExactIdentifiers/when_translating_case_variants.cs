// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers;

public class when_translating_case_variants : given.a_sql_server_query
{
    string[] _first;
    string[] _second;

    void Because()
    {
        _first = Translate("Alice");
        _second = Translate("alice");
    }

    [Fact] void should_preserve_the_upper_case_parameter() => _first.All(_ => _.Contains("= N'Alice'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_lower_case_parameter() => _second.All(_ => _.Contains("= N'alice'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_apply_binary_collation_to_both_spellings() => _first.Concat(_second).All(_ => _.Contains("COLLATE Latin1_General_100_BIN2", StringComparison.Ordinal)).ShouldBeTrue();
}
