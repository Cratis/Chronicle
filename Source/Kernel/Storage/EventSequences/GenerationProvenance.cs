// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Records how a backfilled generation was derived.
/// </summary>
/// <param name="Source">The stored source generation.</param>
/// <param name="SourceIsAppended">Whether the source is the known appended generation.</param>
/// <param name="MigrationsVersion">The migration definitions used.</param>
public record GenerationProvenance(EventTypeGeneration Source, bool SourceIsAppended, EventTypeMigrationsVersion MigrationsVersion);
