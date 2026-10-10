// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Requests an add-only generation backfill on an upgraded worker.
/// </summary>
/// <param name="EventTypeId">The event type to backfill.</param>
public record BackfillEventTypeGenerationsRequest(EventTypeId EventTypeId) : IJobRequest;
