// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQuery.when_a_concurrent_change_interleaves;

public class and_the_item_appears_after_the_check() : given.a_saved_query_store<SaveSequenceQuery>(false, given.StoredOwner.None)
{
    void Establish()
    {
        // The item only appears once the insert has been attempted, whatever else read it before.
        _queries.Get(Arg.Any<SequenceQueryId>()).Returns(_ => _queries.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ISequenceQueryStorage.Insert)) ? given.StoredItems.Query("someone-else") : null);
        _queries.Insert(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>()).Returns(SequenceQueryWriteOutcome.Conflict);
    }

    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQuery("store", "query-1", "Name", SequenceQueryScope.User, string.Empty, "default", "event-log", string.Empty, string.Empty, string.Empty, string.Empty, [], [], null, null, "occurred", false));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query belongs to another owner.");
    [Fact] void should_try_the_insert_once() => _queries.Received(1).Insert(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>());
    [Fact] void should_not_overwrite_what_appeared() => _queries.DidNotReceive().Update(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>(), Arg.Any<SequenceQueryOwner>());
}
