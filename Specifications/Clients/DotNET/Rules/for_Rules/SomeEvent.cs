// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Rules.for_Rules;

[EventType(EventTypeId)]
public record SomeEvent()
{
    public const string EventTypeId = "5ab709aa-bfcb-4599-8ece-6c140bd1d708";
}
