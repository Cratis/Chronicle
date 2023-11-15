// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

[EventType(EventTypeId)]
public record FirstEventType(string Something)
{
    public const string EventTypeId = "d1faeb5d-2951-484a-89d5-bff35a357514";
}
