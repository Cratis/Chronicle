// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections.Outbox;

/// <summary>
/// Represents the definition of outbox projections.
/// </summary>
/// <param name="TargetEventTypeProjections">Projections per target event type.</param>
public record OutboxProjectionsDefinition(IDictionary<EventType, ProjectionDefinition> TargetEventTypeProjections);
