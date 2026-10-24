// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

/// <summary>
/// Long sequence names must not make PostgreSQL truncate or collide parent-history indexes.
/// </summary>
public class when_naming_parent_history_indexes : Specification
{
    string _first;
    string _second;
    string _repeated;

    void Because()
    {
        var commonPrefix = new string('é', 80);
        _first = EventSequenceMigrator.ParentHistoryIndexName(commonPrefix + "-first");
        _second = EventSequenceMigrator.ParentHistoryIndexName(commonPrefix + "-second");
        _repeated = EventSequenceMigrator.ParentHistoryIndexName(commonPrefix + "-first");
    }

    [Fact] void should_fit_the_postgresql_identifier_byte_limit() => (Encoding.UTF8.GetByteCount(_first) <= 63).ShouldBeTrue();
    [Fact] void should_distinguish_sequence_names_with_the_same_long_prefix() => _first.ShouldNotEqual(_second);
    [Fact] void should_use_the_same_name_on_repeated_upgrades() => _repeated.ShouldEqual(_first);
    [Fact] void should_keep_short_table_names_readable() => EventSequenceMigrator.ParentHistoryIndexName("event-sequence").ShouldEqual("IX_event-sequence_parentHistory");
}
