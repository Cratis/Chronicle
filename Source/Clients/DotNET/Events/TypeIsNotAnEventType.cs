// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Exception that gets thrown when a type is not an event type.
/// </summary>
public class TypeIsNotAnEventType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeIsNotAnEventType"/> class.
    /// </summary>
    /// <param name="type">Type that is not an event type.</param>
    public TypeIsNotAnEventType(Type type) : base($"Type '{type.AssemblyQualifiedName}' is not an event type")
    {
    }
}
