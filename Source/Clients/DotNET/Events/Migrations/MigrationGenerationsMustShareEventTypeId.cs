// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// The exception that is thrown when the two generations referenced by an <see cref="EventTypeMigration{TUpgrade, TPrevious}"/>
/// resolve to different event type ids.
/// </summary>
/// <param name="previousType">The previous (older generation) CLR type.</param>
/// <param name="upgradeType">The upgrade (newer generation) CLR type.</param>
/// <param name="previousId">The event type id the previous type resolved to.</param>
/// <param name="upgradeId">The event type id the upgrade type resolved to.</param>
public class MigrationGenerationsMustShareEventTypeId(Type previousType, Type upgradeType, EventTypeId previousId, EventTypeId upgradeId)
    : Exception(
        $"Migration from '{previousType.Name}' (event type '{previousId}') to '{upgradeType.Name}' (event type '{upgradeId}') bridges two different event types. Both generations must resolve to the same event type id - mark the previous generation with [EventTypeGenerationFor<{upgradeType.Name}>(N)] instead of a separate [EventType].");
