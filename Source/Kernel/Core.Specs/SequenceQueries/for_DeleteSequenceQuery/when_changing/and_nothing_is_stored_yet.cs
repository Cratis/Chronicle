// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_DeleteSequenceQuery.when_changing;

public class and_nothing_is_stored_yet() : given.a_saved_query_store<DeleteSequenceQuery>(false, given.StoredOwner.None)
{
    async Task Because() => _result = await _scenario.Execute(new DeleteSequenceQuery("store", "query-1"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_delete_anything() => _queries.DidNotReceive().Delete(Arg.Any<SequenceQueryId>());
}
