// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Exception that gets thrown when a target expression does not resolve to a property.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="TargetPropertyMustBeAProperty"/>.
/// </remarks>
/// <param name="eventType">The event type being configured.</param>
public class TargetPropertyMustBeAProperty(Type eventType)
    : Exception($"The target expression for event type '{eventType.FullName}' must point to a property.");
