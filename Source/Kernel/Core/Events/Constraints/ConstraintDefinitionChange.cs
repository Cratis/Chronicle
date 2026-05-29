// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a change for a specific constraint definition.
/// </summary>
/// <param name="Name">The name of the changed constraint.</param>
/// <param name="RequiresReindex">Whether the change requires reindexing.</param>
/// <param name="ChangeTypes">The specific change types.</param>
public record ConstraintDefinitionChange(
    ConstraintName Name,
    bool RequiresReindex,
    IReadOnlyCollection<ConstraintChangeType> ChangeTypes);
