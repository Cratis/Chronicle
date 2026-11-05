// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.SequenceQueries.for_SaveSequenceQueryFolder.when_changing;

public class and_nothing_is_stored_yet() : given.a_saved_query_store<SaveSequenceQueryFolder>(false, given.StoredOwner.None)
{
    async Task Because() => _result = await _scenario.Execute(new SaveSequenceQueryFolder("store", "folder-1", SequenceQueryScope.User, "default", "some/path"));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_save_it_for_the_caller() => _queries.Received(1).SaveFolder(Arg.Is<SequenceQueryFolderDefinition>(_ => _.Owner.Value != "someone-else"));
}
