// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that can create instances of <see cref="IConstraintValidation"/>.
/// </summary>
public interface IConstraintValidationFactory
{
    /// <summary>
    /// Create a new <see cref="IConstraintValidation"/>.
    /// </summary>
    /// <param name="eventSequenceKey">The <see cref="EventSequenceKey"/> to create the collection for.</param>
    /// <returns>A new instance of <see cref="IConstraintValidation"/>.</returns>
    Task<IConstraintValidation> Create(EventSequenceKey eventSequenceKey);
}
