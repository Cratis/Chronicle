// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.ReadModels;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join;

[ReadModelName("EngagementSummaryWithStringJoin")]
public record EngagementSummary(EventSourceId Id, string CustomerOrgNumber, string CustomerName, EventSequenceNumber __lastHandledEventSequenceNumber);
