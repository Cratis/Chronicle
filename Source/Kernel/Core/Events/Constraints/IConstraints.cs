// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines the constraints system for an event store.
/// </summary>
public interface IConstraints : IGrainWithStringKey
{
    /// <summary>
    /// Register a set of definitions.
    /// </summary>
    /// <param name="definitions">Collection of <see cref="IConstraintDefinition"/> to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<IConstraintDefinition> definitions);
}
