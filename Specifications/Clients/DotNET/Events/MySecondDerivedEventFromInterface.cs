// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

[EventType(EventTypeId)]
public record MySecondDerivedEventFromInterface() : IMyEvent
{
    public const string EventTypeId = "b91c21cd-56db-4edc-805b-b1d1ff9aa772";
    public static EventType EventType = new(EventTypeId, EventGeneration.First);
}
