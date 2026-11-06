// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_DeleteSequenceQueryFolder.when_a_concurrent_change_interleaves;

public class and_the_owner_changes_after_the_check() : given.a_saved_query_store<DeleteSequenceQueryFolder>(false, given.StoredOwner.Caller)
{
    void Establish()
    {
        // Reads before the first write see the first owner; reads after it see the owner that changed meanwhile.
        _queries.GetFolder(Arg.Any<SequenceQueryFolderId>()).Returns(_ => _queries.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ISequenceQueryStorage.DeleteFolder)) ? given.StoredItems.Folder("someone-else") : given.StoredItems.Folder(_storedOwner));
        _queries.DeleteFolder(Arg.Any<SequenceQueryFolderId>(), Arg.Any<SequenceQueryOwner>()).Returns(SequenceQueryWriteOutcome.Conflict);
    }

    async Task Because() => _result = await _scenario.Execute(new DeleteSequenceQueryFolder("store", "folder-1"));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query folder belongs to another owner.");
    [Fact] void should_write_once() => _queries.Received(1).DeleteFolder(Arg.Any<SequenceQueryFolderId>(), Arg.Any<SequenceQueryOwner>());
    [Fact] void should_write_against_the_checked_owner() => _queries.Received(1).DeleteFolder(Arg.Any<SequenceQueryFolderId>(), Arg.Is<SequenceQueryOwner>(_ => _.Value == _storedOwner));
}
