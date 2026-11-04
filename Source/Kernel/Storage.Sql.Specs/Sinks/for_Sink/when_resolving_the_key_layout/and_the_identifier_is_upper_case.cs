// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_resolving_the_key_layout;

public class and_the_identifier_is_upper_case : given.a_sink_over_a_schema_with_an_identifier
{
    protected override string Identifier => "ID";

    object? _read;
    string[] _keys;

    async Task Because()
    {
        await Write("one", "first");
        await Write("two", "second");
        await Write("one", "third");
        await Replay();
        _read = await _sink.FindOrDefault(new Concepts.Keys.Key("one", Properties.ArrayIndexers.NoIndexers));
        _keys = (await _harness.ReadStoredRows()).Select(row => $"{row["ID"]}").Order().ToArray();
    }

    [Fact] void should_read_the_layout_once() => _layoutReads.ShouldEqual(1);
    [Fact] void should_store_the_keys_in_the_identifier_column() => _keys.ShouldEqual(["one", "two"]);
    [Fact] void should_find_the_instance() => _read.ShouldNotBeNull();
    [Fact] void should_read_the_identifier() => ((IDictionary<string, object?>)_read)["ID"].ShouldEqual("one");
}
