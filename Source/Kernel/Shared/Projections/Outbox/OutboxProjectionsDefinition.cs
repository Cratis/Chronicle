// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Outbox;

/// <summary>
/// Represents the definition of outbox projections.
/// </summary>
/// <param name="TargetEventTypeProjections">Projections per target event type.</param>
public record OutboxProjectionsDefinition(IDictionary<EventType, ProjectionDefinition> TargetEventTypeProjections);
