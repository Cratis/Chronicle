// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQuery.when_a_concurrent_change_interleaves;

public class and_the_owner_changes_after_the_check() : given.a_saved_query_store<SaveSequenceQuery>(false, given.StoredOwner.Caller)
{
    void Establish()
    {
        // Reads before the first write see the first owner; reads after it see the owner that changed meanwhile.
        _queries.Get(Arg.Any<SequenceQueryId>()).Returns(_ => _queries.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ISequenceQueryStorage.Update)) ? given.StoredItems.Query("someone-else") : given.StoredItems.Query(_storedOwner));
        _queries.Update(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>(), Arg.Any<SequenceQueryOwner>()).Returns(SequenceQueryWriteOutcome.Conflict);
    }

    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQuery("store", "query-1", "Name", SequenceQueryScope.User, string.Empty, "default", "event-log", string.Empty, string.Empty, string.Empty, string.Empty, [], [], null, null, "occurred", false));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query belongs to another owner.");
    [Fact] void should_write_once() => _queries.Received(1).Update(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>(), Arg.Any<SequenceQueryOwner>());
    [Fact] void should_write_against_the_checked_owner() => _queries.Received(1).Update(Arg.Is<Concepts.SequenceQueries.SequenceQueryDefinition>(_ => _.Owner.Value == _storedOwner), Arg.Is<SequenceQueryOwner>(_ => _.Value == _storedOwner));
}
