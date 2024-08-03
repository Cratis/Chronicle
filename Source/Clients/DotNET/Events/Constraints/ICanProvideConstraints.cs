// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that can provide constraints.
/// </summary>
public interface ICanProvideConstraints
{
    /// <summary>
    /// Provide constraints.
    /// </summary>
    /// <returns>Collection of <see cref="Constraint"/>.</returns>
    IImmutableList<IConstraintDefinition> Provide();
}
