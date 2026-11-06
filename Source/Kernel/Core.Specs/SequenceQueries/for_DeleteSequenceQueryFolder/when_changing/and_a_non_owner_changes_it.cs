// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_DeleteSequenceQueryFolder.when_changing;

public class and_a_non_owner_changes_it() : given.a_saved_query_store<DeleteSequenceQueryFolder>(false, given.StoredOwner.SomeoneElse)
{
    async Task Because() => _result = await _scenario.Execute(new DeleteSequenceQueryFolder("store", "folder-1"));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query folder belongs to another owner.");
    [Fact] void should_not_apply_the_change() => _queries.DidNotReceive().DeleteFolder(Arg.Any<SequenceQueryFolderId>(), Arg.Any<SequenceQueryOwner>());
}
