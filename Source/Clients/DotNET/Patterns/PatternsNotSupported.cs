// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// The exception that is thrown when an event store implementation does not support behavior patterns.
/// </summary>
/// <param name="eventStoreType">The type of event store that does not support behavior patterns.</param>
public class PatternsNotSupported(Type eventStoreType)
    : Exception($"The event store implementation '{eventStoreType.FullName}' does not support behavior patterns.");
