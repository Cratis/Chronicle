// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQuery.when_a_concurrent_change_interleaves;

public class and_the_conflict_clears_on_the_second_read() : given.a_saved_query_store<SaveSequenceQuery>(false, given.StoredOwner.Caller)
{
    void Establish()
    {
        _queries.Get(Arg.Any<SequenceQueryId>()).Returns(given.StoredItems.Query(_storedOwner), given.StoredItems.Query(_storedOwner));
        _queries.Update(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>(), Arg.Any<SequenceQueryOwner>()).Returns(SequenceQueryWriteOutcome.Conflict, SequenceQueryWriteOutcome.Applied);
    }

    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQuery("store", "query-1", "Name", SequenceQueryScope.User, string.Empty, "default", "event-log", string.Empty, string.Empty, string.Empty, string.Empty, [], [], null, null, "occurred", false));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_twice() => _queries.Received(2).Update(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>(), Arg.Any<SequenceQueryOwner>());
}
