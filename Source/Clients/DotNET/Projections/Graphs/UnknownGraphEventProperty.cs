// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Exception that gets thrown when a graph declaration is invalid.
/// </summary>
/// <param name="type">The node or edge type.</param>
/// <param name="eventType">The event type.</param>
/// <param name="property">The name of the property.</param>
public class UnknownGraphEventProperty(Type type, Type eventType, string property) : Exception($"The mapping of '{type.FullName}' refers to the property '{property}', which '{eventType.FullName}' does not have.");
