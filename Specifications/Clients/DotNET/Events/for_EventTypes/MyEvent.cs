// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

[EventType(EventTypeIdentifier)]
public record MyEvent()
{
    public const string EventTypeIdentifier = "9601268f-9a5e-4c4b-86e1-2172a8eea9bc";
}
