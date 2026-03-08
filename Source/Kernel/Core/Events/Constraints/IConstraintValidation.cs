// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that holds a set of <see cref="IConstraintValidator"/> that can validate constraints.
/// </summary>
public interface IConstraintValidation
{
    /// <summary>
    /// Establish a context for a constraint validation.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to establish the context for.</param>
    /// <param name="eventTypeId">The <see cref="EventType"/> to establish the context for.</param>
    /// <param name="content">The content of the event.</param>
    /// <returns>A <see cref="ConstraintValidationContext"/> that can be used for validation.</returns>
    ConstraintValidationContext Establish(EventSourceId eventSourceId, EventTypeId eventTypeId, ExpandoObject content);
}
