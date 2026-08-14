// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries;

/// <summary>
/// Represents the MongoDB document for a folder in the saved query hierarchy.
/// </summary>
/// <param name="Id">The unique identifier of the folder - the primary key.</param>
/// <param name="Scope">Who the folder is visible to.</param>
/// <param name="Owner">The identity that created it.</param>
/// <param name="Namespace">The namespace the folder belongs to.</param>
/// <param name="Path">Where the folder sits within its scope.</param>
public record SequenceQueryFolder(
    string Id,
    SequenceQueryScope Scope,
    string Owner,
    string Namespace,
    string Path);
