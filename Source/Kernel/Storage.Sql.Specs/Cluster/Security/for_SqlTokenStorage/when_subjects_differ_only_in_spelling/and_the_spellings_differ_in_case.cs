// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_SqlTokenStorage.when_subjects_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_case(SqlServerFixture fixture) : given.two_subject_spellings(fixture, "alice", "Alice")
{
    Task Because() => SaveBoth();

    [Fact] void should_be_collapsed_by_the_collation_of_the_database() => _collapsedByCollation.ShouldEqual(1);
    [Fact] void should_find_only_the_tokens_of_the_spelling_by_subject() => OnlyTheFirst(_foundForFirst).ShouldBeTrue();
    [Fact] void should_find_only_the_tokens_of_the_spelling_by_application_and_subject() => OnlyTheFirst(_foundForFirstInApplication).ShouldBeTrue();
    [Fact] void should_find_only_the_tokens_of_the_spelling_by_status() => OnlyTheFirst(_foundForFirstWithStatus).ShouldBeTrue();
    [Fact] void should_find_the_token_of_each_reference() => (_byFirstReference?.Id == "token-one" && _bySecondReference?.Id == "token-two").ShouldBeTrue();
}
