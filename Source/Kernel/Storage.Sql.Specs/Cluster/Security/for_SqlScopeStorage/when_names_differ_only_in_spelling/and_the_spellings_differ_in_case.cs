// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_SqlScopeStorage.when_names_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_case(SqlServerFixture fixture) : given.two_name_spellings(fixture, "alice", "Alice")
{
    Task Because() => SaveFirst();

    [Fact] void should_be_collapsed_by_the_collation_of_the_database() => _collapsedByCollation.ShouldEqual(1);
    [Fact] void should_find_the_stored_spelling_by_name() => _byFirstName.ShouldNotBeNull();
    [Fact] void should_not_find_the_other_spelling_by_name() => _bySecondName.ShouldBeNull();
    [Fact] void should_not_find_the_other_spelling_by_names() => _foundForSecond.ShouldBeEmpty();
}
