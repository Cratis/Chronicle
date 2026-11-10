// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// The exception that is thrown when a children definition declares an ordering the kernel cannot apply.
/// </summary>
/// <param name="projectionId">The <see cref="ProjectionId"/> of the projection declaring the ordering.</param>
/// <param name="childrenProperty">The <see cref="PropertyPath"/> of the children collection.</param>
/// <param name="reason">Why the ordering cannot be applied.</param>
public class InvalidChildrenOrdering(ProjectionId projectionId, PropertyPath childrenProperty, string reason)
    : Exception($"Projection '{projectionId}' declares an ordering for the children at '{childrenProperty}' that cannot be applied: {reason}");
