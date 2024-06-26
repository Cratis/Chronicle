// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when a target property only supports being mapped to a property on the event.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OnlyEventPropertySupported"/> class.
/// </remarks>
/// <param name="targetProperty"><see cref="PropertyPath"/> for the target property that requires this.</param>
public class OnlyEventPropertySupported(PropertyPath targetProperty)
    : Exception($"'{targetProperty}' can only be mapped to another property on event.");
