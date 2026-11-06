// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQuery.when_changing;

public class and_a_non_owner_changes_it() : given.a_saved_query_store<SaveSequenceQuery>(false, given.StoredOwner.SomeoneElse)
{
    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQuery("store", "query-1", "Name", SequenceQueryScope.User, string.Empty, "default", "event-log", string.Empty, string.Empty, string.Empty, string.Empty, [], [], null, null, "occurred", false));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query belongs to another owner.");
    [Fact] void should_not_apply_the_change() => _queries.DidNotReceive().Update(Arg.Any<Concepts.SequenceQueries.SequenceQueryDefinition>(), Arg.Any<SequenceQueryOwner>());
}
