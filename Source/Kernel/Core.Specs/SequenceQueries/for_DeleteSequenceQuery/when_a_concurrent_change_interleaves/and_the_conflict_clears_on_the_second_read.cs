// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_DeleteSequenceQuery.when_a_concurrent_change_interleaves;

public class and_the_conflict_clears_on_the_second_read() : given.a_saved_query_store<DeleteSequenceQuery>(false, given.StoredOwner.Caller)
{
    void Establish()
    {
        _queries.Get(Arg.Any<SequenceQueryId>()).Returns(given.StoredItems.Query(_storedOwner), given.StoredItems.Query(_storedOwner));
        _queries.Delete(Arg.Any<SequenceQueryId>(), Arg.Any<SequenceQueryOwner>()).Returns(SequenceQueryWriteOutcome.Conflict, SequenceQueryWriteOutcome.Applied);
    }

    async Task Because() => _result = await _scenario.Execute(new DeleteSequenceQuery("store", "query-1"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_twice() => _queries.Received(2).Delete(Arg.Any<SequenceQueryId>(), Arg.Any<SequenceQueryOwner>());
}
