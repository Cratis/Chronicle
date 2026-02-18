// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents a version document in MongoDB.
/// </summary>
/// <param name="Id">The document identifier.</param>
/// <param name="Version">The semantic version.</param>
public sealed record VersionDocument(string Id, SemanticVersion Version);
