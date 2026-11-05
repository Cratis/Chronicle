// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_owners_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_width(SqlServerFixture fixture) : given.two_spellings(fixture, "AB", "\uFF21\uFF22")
{
    Task Establish() => Prepare();

    Task Because() => SaveBoth();
    [Fact] void should_be_collapsed_by_the_collation_of_the_database() => _collapsedByCollation.ShouldEqual(1);

    [Fact] void should_let_both_spellings_claim() => (_firstError is null && _secondError is null).ShouldBeTrue();
    [Fact] void should_keep_a_row_for_each_spelling() => _rows.ShouldEqual(2);
    [Fact] void should_refuse_the_other_spelling_for_a_held_value() => (_firstValueRefusedForSecond && _secondValueRefusedForFirst).ShouldBeTrue();
    [Fact] void should_let_a_spelling_keep_its_own_value() => _secondOwnsItsValue.ShouldBeTrue();
    [Fact] void should_read_only_the_claims_of_the_spelling() => _heldBySecond.ShouldContainOnly(_secondValue);
    [Fact] void should_release_only_the_removed_spelling() => (_firstValueAvailableAfterRemoval && _secondValueStillHeld).ShouldBeTrue();
}
