// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Exception that gets thrown when an append definition is built without a when clause.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="MissingAppendCondition"/>.
/// </remarks>
/// <param name="eventType">The event type missing a condition.</param>
public class MissingAppendCondition(Type eventType)
    : Exception($"The append definition for event type '{eventType.FullName}' is missing a when clause.");
