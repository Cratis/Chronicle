// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

[EventType(EventTypeId)]
public record MyFirstDerivedEventFromInterface() : IMyEvent
{
    public const string EventTypeId = "99b9701c-d335-4197-95fe-8ce1e6318185";
    public static EventType EventType = new(EventTypeId, EventGeneration.First);
}
