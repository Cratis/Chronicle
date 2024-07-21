// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Reactions.Reducers;

/// <summary>
/// Represents the definition of an event type with key.
/// </summary>
/// <param name="EventType">Type of event.</param>
/// <param name="Key">Key definition.</param>
public record EventTypeWithKeyExpression(EventType EventType, PropertyExpression Key);
