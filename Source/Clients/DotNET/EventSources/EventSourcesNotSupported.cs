// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// The exception that is thrown when an implementation cannot work with event sources.
/// </summary>
/// <param name="type">The type of the implementation.</param>
public class EventSourcesNotSupported(Type type)
    : Exception($"The implementation '{type.FullName}' does not support event sources.");
