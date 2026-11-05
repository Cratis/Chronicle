// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_subjects_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_width(SqlServerFixture fixture) : given.two_subject_spellings(fixture, "alice", "ａlice")
{
    Task Establish() => Prepare(true);

    Task Because() => SaveAndRemoveFirst();

    [Fact] void should_be_collapsed_by_the_collation_of_the_database() => _collapsedByCollation.ShouldEqual(1);
    [Fact] void should_hold_a_row_for_each_spelling_in_both_table_shapes() => (_sourceRowsBefore == 2 && _valueRowsBefore == 2).ShouldBeTrue();
    [Fact] void should_remove_only_the_rows_of_the_spelling() => (_sourceRowsAfter == 1 && _valueRowsAfter == 1).ShouldBeTrue();
    [Fact] void should_keep_the_other_spelling_claims() => (!_secondSourceValueStillHeld && !_secondValueStillHeld).ShouldBeTrue();
    [Fact] void should_release_the_removed_spelling_claims() => (_firstSourceValueFree && _firstValueFree).ShouldBeTrue();
}
