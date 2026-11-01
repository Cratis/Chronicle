// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// The exception that is thrown when an event type is not declared by the kernel.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownKernelEventType"/> class.
/// </remarks>
/// <param name="eventTypeId">The identifier of the event type.</param>
public class UnknownKernelEventType(EventTypeId eventTypeId)
    : Exception($"Event type '{eventTypeId}' is not an event type declared by the kernel.");
