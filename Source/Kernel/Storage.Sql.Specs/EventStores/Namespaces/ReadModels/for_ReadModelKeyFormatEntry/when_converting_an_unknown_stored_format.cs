// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelKeyFormatEntry;

public class when_converting_an_unknown_stored_format : Specification
{
    Exception? _error;
    ReadModelKeyFormat _missing;
    ReadModelKeyFormat _k1;

    void Because()
    {
        _missing = ReadModelKeyFormatEntry.FromStored("table", null);
        _k1 = ReadModelKeyFormatEntry.FromStored("table", "k1");
        _error = Catch.Exception(() => ReadModelKeyFormatEntry.FromStored("table", "k2"));
    }

    [Fact] void should_treat_a_missing_row_as_legacy() => _missing.ShouldEqual(ReadModelKeyFormat.Legacy);
    [Fact] void should_understand_k1() => _k1.ShouldEqual(ReadModelKeyFormat.K1);
    [Fact] void should_fail_on_a_format_from_a_newer_kernel() => _error.ShouldBeOfExactType<UnknownReadModelKeyFormat>();
}
