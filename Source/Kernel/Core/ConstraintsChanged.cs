// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the payload for when constraints in an event store are changed.
/// </summary>
/// <param name="Changes">The individual constraint definition changes.</param>
public record ConstraintsChanged(IReadOnlyCollection<ConstraintDefinitionChange> Changes);
