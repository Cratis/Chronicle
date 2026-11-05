// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_DeleteSequenceQuery.when_changing;

public class and_an_owner_in_another_case_changes_it() : given.a_saved_query_store<DeleteSequenceQuery>(false, given.StoredOwner.CallerInAnotherCase)
{
    async Task Because() => _result = await _scenario.Execute(new DeleteSequenceQuery("store", "query-1"));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query belongs to another owner.");
    [Fact] void should_not_apply_the_change() => _queries.DidNotReceive().Delete(Arg.Any<SequenceQueryId>());
}
