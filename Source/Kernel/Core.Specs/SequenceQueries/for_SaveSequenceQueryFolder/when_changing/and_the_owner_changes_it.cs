// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQueryFolder.when_changing;

public class and_the_owner_changes_it() : given.a_saved_query_store<SaveSequenceQueryFolder>(false, given.StoredOwner.Caller)
{
    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQueryFolder("store", "folder-1", SequenceQueryScope.User, "default", "some/path"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_apply_the_change() => _queries.Received(1).SaveFolder(Arg.Any<SequenceQueryFolderDefinition>());
    [Fact] void should_keep_the_stored_owner() => _queries.Received(1).SaveFolder(Arg.Is<SequenceQueryFolderDefinition>(_ => _.Owner.Value == _storedOwner));
}
