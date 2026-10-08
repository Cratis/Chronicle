// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// The exception that is thrown when an event type with key expression received from a client has no event type.
/// </summary>
/// <param name="key">The key expression that came without an event type.</param>
public class MissingEventTypeForKeyExpression(string? key)
    : Exception($"An event type with key expression '{key}' was received without an event type. Every event type an observer registers must carry its event type.");
