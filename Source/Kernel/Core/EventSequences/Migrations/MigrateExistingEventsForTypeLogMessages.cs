// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Log messages for event type generation migration.
/// </summary>
internal static partial class MigrateExistingEventsForTypeLogMessages
{
    [LoggerMessage(LogLevel.Information, "Migration completed successfully for event type {EventTypeId}")]
    internal static partial void MigrationCompleted(this ILogger logger, EventTypeId eventTypeId);

    [LoggerMessage(LogLevel.Warning, "Migration completed with failures for event type {EventTypeId}")]
    internal static partial void MigrationFailed(this ILogger logger, EventTypeId eventTypeId);

    [LoggerMessage(LogLevel.Information, "Migrating event at sequence number {SequenceNumber} for event type {EventTypeId}")]
    internal static partial void MigratingEvent(this ILogger logger, EventSequenceNumber sequenceNumber, EventTypeId eventTypeId);

    [LoggerMessage(LogLevel.Information, "Starting migration job for event type {EventTypeId} generation {Generation} in event store {EventStore}")]
    internal static partial void StartingMigrationJob(this ILogger logger, EventTypeId eventTypeId, EventTypeGeneration generation, EventStoreName eventStore);
}
