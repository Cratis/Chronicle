// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Requests an add-only generation backfill on an upgraded worker.
/// </summary>
/// <param name="EventTypeId">The event type to backfill.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.EventSequences.Migrations.BackfillEventTypeGenerationsRequest")]
public record BackfillEventTypeGenerationsRequest([Id(0)] EventTypeId EventTypeId) : IJobRequest;
