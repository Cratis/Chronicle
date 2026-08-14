// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.InMemory.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage;

/// <summary>
/// A folder is only visible to the identity that created it, unless it was shared with everyone -
/// the same rule the queries filed into it follow.
/// </summary>
public class when_saving_folders : Specification
{
    static readonly SequenceQueryOwner _alice = new("alice");
    static readonly SequenceQueryOwner _bob = new("bob");

    SequenceQueryStorage _storage;
    IEnumerable<SequenceQueryFolderDefinition> _visibleToAlice;
    IEnumerable<SequenceQueryFolderDefinition> _visibleToBob;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.SaveFolder(new("private", SequenceQueryScope.User, _alice, "default", "Diagnostics"));
        await _storage.SaveFolder(new("shared", SequenceQueryScope.Everyone, _alice, "default", "Reporting"));

        _visibleToAlice = await _storage.GetAllFoldersFor(_alice);
        _visibleToBob = await _storage.GetAllFoldersFor(_bob);
    }

    [Fact] void should_show_the_owner_both_folders() => _visibleToAlice.Count().ShouldEqual(2);
    [Fact] void should_show_everybody_else_only_the_shared_one() => _visibleToBob.Count().ShouldEqual(1);
    [Fact] void should_show_everybody_else_the_shared_path() => _visibleToBob.Single().Path.Value.ShouldEqual("Reporting");

    void Destroy() => _storage.Dispose();
}
