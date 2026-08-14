// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.InMemory.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage;

public class when_deleting_a_folder : Specification
{
    static readonly SequenceQueryOwner _alice = new("alice");

    SequenceQueryStorage _storage;
    IEnumerable<SequenceQueryFolderDefinition> _remaining;

    async Task Establish()
    {
        _storage = new();
        await _storage.SaveFolder(new("first", SequenceQueryScope.User, _alice, "default", "Diagnostics"));
        await _storage.SaveFolder(new("second", SequenceQueryScope.User, _alice, "default", "Reporting"));
    }

    async Task Because()
    {
        await _storage.DeleteFolder(new SequenceQueryFolderId("first"));
        _remaining = await _storage.GetAllFoldersFor(_alice);
    }

    [Fact] void should_leave_the_other_folder() => _remaining.Count().ShouldEqual(1);
    [Fact] void should_leave_the_one_not_deleted() => _remaining.Single().Id.Value.ShouldEqual("second");

    void Destroy() => _storage.Dispose();
}
