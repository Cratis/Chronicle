// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents the request for migrating existing events of a specific event type
/// when a new generation has been added.
/// </summary>
/// <param name="EventTypeId">The <see cref="Concepts.Events.EventTypeId"/> of the event type to migrate.</param>
public record MigrateExistingEventsForTypeRequest(EventTypeId EventTypeId) : IJobRequest;
