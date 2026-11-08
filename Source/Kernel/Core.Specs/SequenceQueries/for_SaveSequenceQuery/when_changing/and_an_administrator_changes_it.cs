// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQuery.when_changing;

public class and_an_administrator_changes_it() : given.a_saved_query_store<SaveSequenceQuery>(true, given.StoredOwner.SomeoneElse)
{
    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQuery("store", "query-1", "Name", SequenceQueryScope.User, string.Empty, "default", "event-log", string.Empty, string.Empty, string.Empty, string.Empty, [], [], null, null, "occurred", false));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_apply_the_change() => _queries.Received(1).Update(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>(), Arg.Any<SequenceQueryOwner>());
    [Fact] void should_keep_the_stored_owner() => _queries.Received(1).Update(Arg.Is<Concepts.SequenceQueries.SequenceQueryDefinition>(_ => _.Owner.Value == _storedOwner), Arg.Is<SequenceQueryOwner>(_ => _.Value == _storedOwner));
}
