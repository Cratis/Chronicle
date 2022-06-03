// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Exception that gets thrown when a type should be marked with the <see cref="EventTypeAttribute"/>.
/// </summary>
public class MissingEventTypeAttribute : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEventTypeAttribute"/> class.
    /// </summary>
    /// <param name="type">Type that is missing the attribute.</param>
    public MissingEventTypeAttribute(Type type) : base($"Type '{type.FullName}' is missing the [EventType(\"<Guid>\")] attribute.")
    {
    }
}
