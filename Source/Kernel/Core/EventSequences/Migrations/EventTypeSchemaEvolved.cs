// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.EventSequences.Migrations;

/// <summary>
/// Records an in-place schema evolution on the System event sequence, without starting a migration.
/// </summary>
/// <param name="EventTypeId">The evolved event type.</param>
/// <param name="Generation">The existing generation.</param>
/// <param name="PreviousSchema">The exact schema replaced by the conditional write.</param>
/// <param name="Schema">The schema now registered.</param>
/// <param name="Classification">The compatibility classification.</param>
[EventType, AllEventStores]
public record EventTypeSchemaEvolved(EventTypeId EventTypeId, EventTypeGeneration Generation, string PreviousSchema, string Schema, string Classification);
