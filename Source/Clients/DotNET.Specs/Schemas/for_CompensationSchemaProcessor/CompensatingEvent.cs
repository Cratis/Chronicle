// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Schemas.for_CompensationSchemaProcessor;

[EventType]
[CompensationFor<OriginalEvent>]
public class CompensatingEvent
{
    public string Value { get; init; } = string.Empty;
}
