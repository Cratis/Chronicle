// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Attribute used to mark a type as representing a previous generation of another event type.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EventTypeGenerationForAttribute"/>.
/// </remarks>
/// <param name="generation">The generation this type represents.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public abstract class EventTypeGenerationForAttribute(uint generation) : Attribute
{
    /// <summary>
    /// Gets the <see cref="EventTypeGeneration"/> this type represents.
    /// </summary>
    public EventTypeGeneration Generation { get; } = generation;

    /// <summary>
    /// Gets the <see cref="Type"/> of the event type this is a generation for.
    /// </summary>
    public abstract Type EventTypeClrType { get; }
}
