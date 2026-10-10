// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// The exception that is thrown when a child join omits its target but the child has no identifying property.
/// </summary>
/// <param name="eventType">The event type being joined.</param>
/// <param name="memberName">The child member being populated.</param>
public class MissingChildIdentityForJoin(Type eventType, string memberName)
    : Exception($"Join from '{eventType.Name}' for child member '{memberName}' requires an identifying property. Add a [Key] property, specify ChildrenFrom identifiedBy, or specify Join on explicitly; the event source id expression is not a stored child property.");
