// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Exception that gets thrown when a type is not an event type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeIsNotAnEventType"/> class.
/// </remarks>
/// <param name="type">Type that is not an event type.</param>
public class TypeIsNotAnEventType(Type type) : Exception($"Type '{type.AssemblyQualifiedName}' is not an event type. You need to add a [EventType] attribute to it.");
