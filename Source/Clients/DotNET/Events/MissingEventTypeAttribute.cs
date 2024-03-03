// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events;

/// <summary>
/// Exception that gets thrown when a type should be marked with the <see cref="EventTypeAttribute"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingEventTypeAttribute"/> class.
/// </remarks>
/// <param name="type">Type that is missing the attribute.</param>
public class MissingEventTypeAttribute(Type type) : Exception($"Type '{type.FullName}' is missing the [EventType(\"<Guid>\")] attribute.")
{
}
