// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Exception that gets thrown when an event type does not have a mapped CLR type.
/// </summary>
public class MissingClrTypeForEventType : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEventTypeForClrType"/> class.
    /// </summary>
    /// <param name="eventTypeId">The event type identifier.</param>
    public MissingClrTypeForEventType(EventTypeId eventTypeId) : base($"Missing mapping from event type '{eventTypeId}' to a CLR type. Has the type been adorned with an [EventType(\"<guid>\")] attribute?")
    {
    }
}
