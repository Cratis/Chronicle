// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents the state for <see cref="Constraints"/>.
/// </summary>
public class ConstraintsState
{
    /// <summary>
    /// Gets or sets the constraints.
    /// </summary>
    public IList<IConstraintDefinition> Constraints { get; set; } = [];
}
