// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents the state of an add-only generation backfill step.
/// </summary>
public class BackfillEventTypeGenerationsStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the event type being backfilled.
    /// </summary>
    public EventTypeId EventTypeId { get; set; } = EventTypeId.Unknown;
}
