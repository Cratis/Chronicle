// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception thrown when different local projection owners declare the same identifier.
/// </summary>
/// <param name="id">The shared projection identifier.</param>
/// <param name="owners">The CLR types owning the projections.</param>
public class MultipleProjectionsWithSameIdFound(ProjectionId id, IEnumerable<Type> owners)
    : Exception($"Multiple projections with id '{id}' found: {string.Join(", ", owners.Select(_ => _.FullName))}. Each local projection identifier must have exactly one owner.");
