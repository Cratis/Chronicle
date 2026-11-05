// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_value_owners_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_width(SqlServerFixture fixture) : given.two_value_spellings(fixture, "Alice", "Ａlice")
{
    Task Establish() => Prepare(true);

    Task Because() => SaveAndRemove();

    [Fact] void should_be_collapsed_by_the_collation_of_the_database() => _collapsedByCollation.ShouldEqual(1);
    [Fact] void should_let_both_spellings_claim() => (_firstError is null && _secondError is null).ShouldBeTrue();
    [Fact] void should_keep_a_row_for_each_spelling() => _rows.ShouldEqual(2);
    [Fact] void should_refuse_the_other_spelling_for_a_held_value() => _otherSpellingMayTakeTheValue.ShouldBeFalse();
    [Fact] void should_read_only_the_claims_of_a_spelling() => _heldByFirst.ShouldContainOnly(_firstValue);
    [Fact] void should_stream_only_the_claims_of_a_spelling() => _streamedBySecond.ShouldContainOnly(_secondValue);
    [Fact] void should_not_release_a_value_for_another_spelling() => _rowsAfterForeignRemoveValue.ShouldEqual(2);
    [Fact] void should_release_a_value_for_its_own_spelling() => _rowsAfterRemoveValue.ShouldEqual(1);
    [Fact] void should_keep_the_other_spelling_claim_after_a_removal() => (_secondStillOwnsItsValue && _rowsAfterRemove == 1).ShouldBeTrue();
    [Fact] void should_leave_a_released_value_free() => _firstValueFree.ShouldBeTrue();
}
