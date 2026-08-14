// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.SequenceQueries;

/// <summary>
/// Represents a folder in the saved query hierarchy.
/// </summary>
/// <param name="Id">The unique identifier of the folder.</param>
/// <param name="Scope">Who the folder is visible to.</param>
/// <param name="Owner">The identity that created it.</param>
/// <param name="Namespace">The namespace the folder belongs to.</param>
/// <param name="Path">Where the folder sits within its scope.</param>
/// <remarks>
/// Folders are stored in their own right rather than derived from the paths the queries carry,
/// because a folder that holds nothing yet has no query to be inferred from - and creating a folder
/// before deciding what goes in it is the normal order of doing things.
/// </remarks>
public record SequenceQueryFolderDefinition(
    SequenceQueryFolderId Id,
    SequenceQueryScope Scope,
    SequenceQueryOwner Owner,
    EventStoreNamespaceName Namespace,
    SequenceQueryFolder Path);
