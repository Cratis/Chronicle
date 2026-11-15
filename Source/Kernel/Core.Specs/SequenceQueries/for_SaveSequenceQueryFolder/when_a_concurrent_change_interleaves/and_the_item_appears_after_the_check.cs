// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQueryFolder.when_a_concurrent_change_interleaves;

public class and_the_item_appears_after_the_check() : given.a_saved_query_store<SaveSequenceQueryFolder>(false, given.StoredOwner.None)
{
    void Establish()
    {
        // The item only appears once the insert has been attempted, whatever else read it before.
        _queries.GetFolder(Arg.Any<SequenceQueryFolderId>()).Returns(_ => _queries.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(ISequenceQueryStorage.InsertFolder)) ? given.StoredItems.Folder("someone-else") : null);
        _queries.InsertFolder(Arg.Any<SequenceQueryFolderDefinition>()).Returns(SequenceQueryWriteOutcome.Conflict);
    }

    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQueryFolder("store", "folder-1", SequenceQueryScope.User, "default", "some/path"));

    [Fact] void should_be_refused() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The saved query folder belongs to another owner.");
    [Fact] void should_try_the_insert_once() => _queries.Received(1).InsertFolder(Arg.Any<SequenceQueryFolderDefinition>());
    [Fact] void should_not_overwrite_what_appeared() => _queries.DidNotReceive().UpdateFolder(Arg.Any<SequenceQueryFolderDefinition>(), Arg.Any<SequenceQueryOwner>());
}
