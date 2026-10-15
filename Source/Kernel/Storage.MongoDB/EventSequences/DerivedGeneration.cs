// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents the persisted provenance of a backfilled generation.
/// </summary>
/// <param name="SourceGeneration">The source generation.</param>
/// <param name="SourceIsAppended">Whether the source is the known appended generation.</param>
/// <param name="MigrationsVersion">The migration definition digest.</param>
public record DerivedGeneration(uint SourceGeneration, bool SourceIsAppended, string MigrationsVersion);
