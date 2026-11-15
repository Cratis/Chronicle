// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQueryFolder.when_a_concurrent_change_interleaves;

public class and_the_conflict_clears_on_the_second_read() : given.a_saved_query_store<SaveSequenceQueryFolder>(false, given.StoredOwner.Caller)
{
    void Establish()
    {
        _queries.GetFolder(Arg.Any<SequenceQueryFolderId>()).Returns(given.StoredItems.Folder(_storedOwner), given.StoredItems.Folder(_storedOwner));
        _queries.UpdateFolder(Arg.Any<SequenceQueryFolderDefinition>(), Arg.Any<SequenceQueryOwner>()).Returns(SequenceQueryWriteOutcome.Conflict, SequenceQueryWriteOutcome.Applied);
    }

    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQueryFolder("store", "folder-1", SequenceQueryScope.User, "default", "some/path"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_twice() => _queries.Received(2).UpdateFolder(Arg.Any<SequenceQueryFolderDefinition>(), Arg.Any<SequenceQueryOwner>());
}
