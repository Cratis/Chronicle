// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;

/// <summary>
/// Represents an immutable migration set recorded for backfill provenance.
/// </summary>
/// <param name="Migrations">The executable migration definitions.</param>
/// <param name="Recorded">When this version was first recorded.</param>
public record MigrationVersion(IEnumerable<EventTypeMigration> Migrations, DateTimeOffset Recorded);
