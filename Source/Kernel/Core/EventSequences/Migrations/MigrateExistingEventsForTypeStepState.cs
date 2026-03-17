// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents the state for a <see cref="IMigrateExistingEventsForTypeStep"/> job step.
/// </summary>
public class MigrateExistingEventsForTypeStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the <see cref="EventTypeId"/> of the event type being migrated.
    /// </summary>
    public EventTypeId EventTypeId { get; set; } = EventTypeId.Unknown;
}
