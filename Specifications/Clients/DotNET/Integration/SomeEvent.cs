// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Integration;

[EventType(EventTypeId)]
public record SomeEvent(int SomeInteger, string SomeString)
{
    public const string EventTypeId = "ebe9cc8b-a0bd-4357-9aff-2edb545c868d";
}
