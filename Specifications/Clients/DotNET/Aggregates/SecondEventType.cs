// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

[EventType(EventTypeId)]
public record SecondEventType(string Something)
{
    public const string EventTypeId = "be1c9989-7bae-4947-af10-07f90581b63c";
}
