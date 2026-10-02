// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an event sequence implementation cannot append through event sources.
/// </summary>
/// <param name="type">The type of the event sequence implementation.</param>
public class EventSourcesNotSupported(Type type)
    : Exception($"The event sequence '{type.FullName}' does not support appending through event sources.");
