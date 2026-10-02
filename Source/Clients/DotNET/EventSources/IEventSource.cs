// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Marks a type as the definition of an event source, what it is and which event streams it has.
/// </summary>
/// <remarks>
/// The type is never instantiated. It carries an <see cref="EventSourceAttribute"/> and any number of
/// <see cref="EventStreamAttribute"/>, is discovered at startup and is registered with the Kernel.
/// </remarks>
public interface IEventSource;
