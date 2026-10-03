// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// The exception that is thrown when an event source definition declares the same stream name more than once.
/// </summary>
/// <param name="type">The event source type.</param>
/// <param name="name">The duplicated stream name.</param>
public class DuplicateEventStreamName(Type type, string name)
    : Exception($"The event source '{type.FullName}' declares the event stream '{name}' more than once.");
