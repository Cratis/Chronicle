// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents the context for constraint validation.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to validate for.</param>
/// <param name="EventType">The <see cref="EventType"/> to validate for.</param>
/// <param name="Content">The content of the event.</param>
public record ConstraintValidationContext(EventSourceId EventSourceId, EventType EventType, ExpandoObject Content);
