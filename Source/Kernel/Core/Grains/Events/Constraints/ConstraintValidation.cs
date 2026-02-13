// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IConstraintValidation"/>.
/// </summary>
/// <param name="validators">Collection of <see cref="IConstraintValidator"/> to use.</param>
public class ConstraintValidation(IEnumerable<IConstraintValidator> validators) : IConstraintValidation
{
    /// <inheritdoc/>
    public ConstraintValidationContext Establish(EventSourceId eventSourceId, EventTypeId eventTypeId, ExpandoObject content) =>
        new(validators, eventSourceId, eventTypeId, content);
}
