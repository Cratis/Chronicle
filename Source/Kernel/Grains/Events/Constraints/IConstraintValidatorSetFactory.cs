// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Defines a system that can create instances of <see cref="IConstraintValidatorSet"/>.
/// </summary>
public interface IConstraintValidatorSetFactory
{
    /// <summary>
    /// Create a new <see cref="IConstraintValidatorSet"/>.
    /// </summary>
    /// <param name="eventSequenceKey">The <see cref="EventSequenceKey"/> to create the collection for.</param>
    /// <returns>A new instance of <see cref="IConstraintValidatorSet"/>.</returns>
    Task<IConstraintValidatorSet> Create(EventSequenceKey eventSequenceKey);
}
