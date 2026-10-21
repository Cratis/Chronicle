// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Reads model-bound closing declarations from event types.
/// </summary>
public static class ClosesStreamAttributeExtensions
{
    /// <summary>
    /// Get the closing declaration applied to an event type.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>The declaration, or null if none is applied.</returns>
    public static ClosesStreamAttribute? GetClosesStream(this Type eventType) => eventType.GetCustomAttribute<ClosesStreamAttribute>();
}
