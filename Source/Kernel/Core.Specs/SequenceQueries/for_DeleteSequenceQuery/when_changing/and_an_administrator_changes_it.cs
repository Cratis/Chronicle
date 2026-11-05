// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_DeleteSequenceQuery.when_changing;

public class and_an_administrator_changes_it() : given.a_saved_query_store<DeleteSequenceQuery>(true, given.StoredOwner.SomeoneElse)
{
    async Task Because() => _result = await _scenario.Execute(new DeleteSequenceQuery("store", "query-1"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_apply_the_change() => _queries.Received(1).Delete(Arg.Any<SequenceQueryId>());
}
