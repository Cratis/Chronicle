// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage.given;

/// <summary>
/// Reading back what a storage holds, for the specs.
/// </summary>
public static class StoredItemExtensions
{
    /// <summary>
    /// Check what a stored query holds.
    /// </summary>
    /// <param name="query">The stored query.</param>
    /// <param name="name">The expected name.</param>
    /// <param name="owner">The expected owner.</param>
    public static void ShouldBeStoredAs(this SequenceQueryDefinition query, string name, string owner)
    {
        query.Name.Value.ShouldEqual(name);
        query.Owner.Value.ShouldEqual(owner);
    }

    /// <summary>
    /// Check what a stored folder holds.
    /// </summary>
    /// <param name="folder">The stored folder.</param>
    /// <param name="owner">The expected owner.</param>
    /// <param name="path">The expected path.</param>
    public static void ShouldBeStoredAs(this SequenceQueryFolderDefinition folder, string owner, string path)
    {
        folder.Owner.Value.ShouldEqual(owner);
        folder.Path.Value.ShouldEqual(path);
    }
}
