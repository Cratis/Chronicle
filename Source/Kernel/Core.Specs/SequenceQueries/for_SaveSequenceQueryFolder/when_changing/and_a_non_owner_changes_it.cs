// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQueryFolder.when_changing;

public class and_a_non_owner_changes_it() : given.a_saved_query_store<SaveSequenceQueryFolder>(false, given.StoredOwner.SomeoneElse)
{
    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQueryFolder("store", "folder-1", SequenceQueryScope.User, "default", "some/path"));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query folder belongs to another owner.");
    [Fact] void should_not_apply_the_change() => _queries.DidNotReceive().SaveFolder(Arg.Any<SequenceQueryFolderDefinition>());
}
